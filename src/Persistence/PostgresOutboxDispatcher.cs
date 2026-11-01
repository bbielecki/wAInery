using System.Collections.Immutable;
using Npgsql;
using wAInery.BuildingBlocks;

namespace wAInery.Persistence;

// Implementations perform short DB-only effects in this supplied transaction.
public interface IOutboxConsumer
{
    string Name { get; }
    Task HandleAsync(DomainChangeV1 change, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct);
}

public sealed class PostgresOutboxDispatcher
{
    private sealed record Registration(string Name, IOutboxConsumer Consumer);
    private sealed record Pending(Guid Id, WorkspaceId Workspace, string Payload, string EventType,
        long AggregateVersion, int SchemaVersion, int Attempts);
    private readonly NpgsqlDataSource source;
    private readonly ImmutableArray<Registration> consumers;
    private readonly int attemptLimit;

    public PostgresOutboxDispatcher(NpgsqlDataSource dataSource, IEnumerable<IOutboxConsumer> consumers, int maxAttempts = 5)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(consumers);
        var registrations = consumers.Select(consumer => consumer is not null
            ? new Registration(consumer.Name, consumer) : throw new ArgumentException("Consumer is required.")).ToImmutableArray();
        if (registrations.IsEmpty || registrations.Any(item => string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 128)
            || registrations.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != registrations.Length)
            throw new ArgumentException("Unique stable consumer names are required.");
        if (maxAttempts is < 1 or > 20) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        source = dataSource;
        this.consumers = registrations;
        attemptLimit = maxAttempts;
    }

    public async Task<bool> DispatchOneAsync(CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        Pending pending;
        await using (var select = new NpgsqlCommand("""
            SELECT id,workspace_id,payload::text,event_type,aggregate_version,schema_version,attempts
            FROM processing.outbox WHERE delivered_at IS NULL AND dead_at IS NULL AND available_at<=clock_timestamp()
            ORDER BY available_at,id FOR UPDATE SKIP LOCKED LIMIT 1
            """, connection, transaction) { CommandTimeout = 30 })
        await using (var reader = await select.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) return false;
            pending = new(reader.GetGuid(0), new(reader.GetGuid(1)), reader.GetString(2), reader.GetString(3),
                reader.GetInt64(4), reader.GetInt32(5), reader.GetInt32(6));
        }
        if (pending.Attempts >= attemptLimit)
        {
            await using var exhaust = new NpgsqlCommand("""
                UPDATE processing.outbox SET dead_at=clock_timestamp(),last_error='ATTEMPTS_EXHAUSTED' WHERE id=@id
                """, connection, transaction) { CommandTimeout = 30 };
            exhaust.Parameters.AddWithValue("id", pending.Id);
            await exhaust.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }

        // Lock belongs to outer tx; retry bookkeeping cannot race a reclaimed row.
        await transaction.SaveAsync("consumer_effects", ct);
        var failureCode = "INVALID_PAYLOAD";
        try
        {
            var change = DomainChangeV1Json.Deserialize(pending.Payload);
            if (change.EventId != pending.Id || change.Workspace != pending.Workspace || change.EventType != pending.EventType
                || change.AggregateVersion != pending.AggregateVersion || change.SchemaVersion != pending.SchemaVersion)
                throw new InvalidDataException("Outbox metadata does not match its envelope.");
            failureCode = "CONSUMER_FAILURE";
            foreach (var registration in consumers)
            {
                ct.ThrowIfCancellationRequested();
                await using var inbox = new NpgsqlCommand("""
                    INSERT INTO processing.inbox_dedupe(consumer,event_id) VALUES (@consumer,@event)
                    ON CONFLICT(consumer,event_id) DO NOTHING RETURNING event_id
                    """, connection, transaction) { CommandTimeout = 30 };
                inbox.Parameters.AddWithValue("consumer", registration.Name);
                inbox.Parameters.AddWithValue("event", pending.Id);
                if (await inbox.ExecuteScalarAsync(ct) is Guid)
                    await registration.Consumer.HandleAsync(change, connection, transaction, ct);
            }
            ct.ThrowIfCancellationRequested();
            await using var ack = new NpgsqlCommand("""
                UPDATE processing.outbox SET delivered_at=clock_timestamp(),attempts=attempts+1,last_error=NULL WHERE id=@id
                """, connection, transaction) { CommandTimeout = 30 };
            ack.Parameters.AddWithValue("id", pending.Id);
            await ack.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // A SQL failure aborts the transaction too; restore it before recording
            // only a controlled code. No exception text/private payload is logged.
            await transaction.RollbackAsync("consumer_effects", ct);
            await using var retry = new NpgsqlCommand("""
                UPDATE processing.outbox SET attempts=attempts+1,
                  available_at=clock_timestamp()+make_interval(secs=>least(300,power(2,attempts+1))::double precision),
                  dead_at=CASE WHEN attempts+1>=@limit THEN clock_timestamp() ELSE NULL END,last_error=@error
                WHERE id=@id
                """, connection, transaction) { CommandTimeout = 30 };
            retry.Parameters.AddWithValue("id", pending.Id);
            retry.Parameters.AddWithValue("limit", attemptLimit);
            retry.Parameters.AddWithValue("error", failureCode);
            await retry.ExecuteNonQueryAsync(ct);
            await transaction.CommitAsync(ct);
            return true;
        }
    }
}
