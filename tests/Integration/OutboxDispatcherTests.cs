using Npgsql;
using NpgsqlTypes;
using wAInery.BuildingBlocks;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

// Consumers are synthetic SQL transaction probes, not Decisions/owner handlers.
public sealed class OutboxDispatcherTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly WorkspaceId Workspace = new(Guid.Parse("00000000-0000-0000-0000-000000000021"));
    private static readonly WorkspaceId OtherWorkspace = new(Guid.Parse("00000000-0000-0000-0000-000000000022"));
    private static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-31T10:00:00Z");
    private const string PrivateMarker = "SYNTHETIC_DISPATCHER_PRIVATE_MARKER";

    [Theory]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("blank")]
    [InlineData("attempts-zero")]
    [InlineData("attempts-high")]
    public async Task Invalid_consumer_configuration_is_rejected(string invalidPart)
    {
        await using var source = await PrepareDatabase();
        var consumers = new List<IOutboxConsumer> { Probe("fixture-consumer") };
        var maxAttempts = 5;
        if (invalidPart == "empty") consumers.Clear();
        if (invalidPart == "duplicate") consumers.Add(Probe("fixture-consumer"));
        if (invalidPart == "blank") consumers = [Probe(" ")];
        if (invalidPart == "attempts-zero") maxAttempts = 0;
        if (invalidPart == "attempts-high") maxAttempts = 21;
        Assert.ThrowsAny<ArgumentException>(() => new PostgresOutboxDispatcher(source, consumers, maxAttempts));
    }

    [Fact]
    public async Task Effect_inbox_and_ack_commit_once_even_after_forced_duplicate_delivery()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        var dispatcher = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]);
        Assert.True(await dispatcher.DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
        var state = await ReadState(source, change.EventId);
        Assert.NotNull(state.DeliveredAt);
        Assert.Null(state.DeadAt);
        Assert.Equal(1, state.Attempts);
        Assert.False(await dispatcher.DispatchOneAsync(Ct));

        // Simulate at-least-once replay of an already acknowledged fixture event.
        await ForceDue(source, change.EventId, resetDelivered: true);
        Assert.True(await new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
        Assert.NotNull((await ReadState(source, change.EventId)).DeliveredAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handler_failure_rolls_effect_and_inbox_back_and_fresh_dispatcher_recovers(bool sqlFailure)
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        var before = await DatabaseNow(source);
        var failed = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer", fail: !sqlFailure, sqlFailure: sqlFailure)]);
        Assert.True(await failed.DispatchOneAsync(Ct));
        var state = await ReadState(source, change.EventId);
        await AssertCounts(source, effects: 0, inboxes: 0);
        Assert.Null(state.DeliveredAt);
        Assert.Null(state.DeadAt);
        Assert.Equal(1, state.Attempts);
        Assert.False(string.IsNullOrWhiteSpace(state.LastError));
        Assert.DoesNotContain(PrivateMarker, state.LastError!);
        Assert.True(state.AvailableAt >= before.AddSeconds(2));
        Assert.True(state.AvailableAt <= state.DatabaseNow.AddSeconds(2));

        await ForceDue(source, change.EventId);
        Assert.True(await new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
        Assert.NotNull((await ReadState(source, change.EventId)).DeliveredAt);
    }

    [Fact]
    public async Task Failure_of_second_consumer_rolls_all_fanout_effects_back_before_retry()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        var failed = new PostgresOutboxDispatcher(source,
            [Probe("fixture-first"), Probe("fixture-second", fail: true)]);
        Assert.True(await failed.DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 0, inboxes: 0);
        Assert.Null((await ReadState(source, change.EventId)).DeliveredAt);
        await ForceDue(source, change.EventId);
        var healthy = new PostgresOutboxDispatcher(source,
            [Probe("fixture-first"), Probe("fixture-second")]);
        Assert.True(await healthy.DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 2, inboxes: 2);
        Assert.NotNull((await ReadState(source, change.EventId)).DeliveredAt);
    }

    [Fact]
    public async Task Shared_job_enqueue_uses_the_supplied_transaction_and_rolls_back_with_consumer()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        var queue = new PostgresJobQueue(source);
        IOutboxConsumer JobConsumer(bool fail) => new SqlConsumer("fixture-jobs", async (evt, connection, transaction, ct) =>
        {
            await queue.EnqueueAsync(connection, transaction,
                new JobRequest(evt.Workspace, "fixture-recompute", evt.Subject.Id, evt.AggregateVersion, At), ct);
            await InsertEffect("fixture-jobs", evt, connection, transaction, ct);
            if (fail) throw new InvalidOperationException(PrivateMarker);
        });
        Assert.True(await new PostgresOutboxDispatcher(source, [JobConsumer(true)]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 0, inboxes: 0);
        Assert.Equal(0, await Count(source, "SELECT count(*) FROM processing.jobs"));
        Assert.Null((await ReadState(source, change.EventId)).DeliveredAt);

        await ForceDue(source, change.EventId);
        Assert.True(await new PostgresOutboxDispatcher(source, [JobConsumer(false)]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
        Assert.Equal(1, await Count(source, "SELECT count(*) FROM processing.jobs WHERE status='PENDING' AND desired_revision=1"));
        await ForceDue(source, change.EventId, resetDelivered: true);
        Assert.True(await new PostgresOutboxDispatcher(source, [JobConsumer(false)]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
        Assert.Equal(1, await Count(source, "SELECT count(*) FROM processing.jobs"));
    }

    [Fact]
    public async Task A_future_event_remains_pending_without_a_processing_attempt()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        await using (var connection = await source.OpenConnectionAsync(Ct))
        await using (var schedule = new NpgsqlCommand("UPDATE processing.outbox SET available_at=clock_timestamp()+interval '1 hour' WHERE id=@id", connection))
        {
            schedule.Parameters.AddWithValue("id", change.EventId);
            await schedule.ExecuteNonQueryAsync(Ct);
        }
        Assert.False(await new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]).DispatchOneAsync(Ct));
        var state = await ReadState(source, change.EventId);
        Assert.Equal(0, state.Attempts);
        Assert.Null(state.DeliveredAt);
        await AssertCounts(source, effects: 0, inboxes: 0);
    }

    [Fact]
    public async Task Registration_enumeration_is_snapshotted_before_processing()
    {
        await using var source = await PrepareDatabase();
        await SeedEvent(source);
        var registrations = new List<IOutboxConsumer> { Probe("fixture-original") };
        var dispatcher = new PostgresOutboxDispatcher(source, registrations);
        registrations.Clear();
        registrations.Add(Probe("fixture-late"));
        Assert.True(await dispatcher.DispatchOneAsync(Ct));
        Assert.Equal(1, await Count(source, "SELECT count(*) FROM processing.dispatch_probe WHERE consumer='fixture-original'"));
        Assert.Equal(0, await Count(source, "SELECT count(*) FROM processing.dispatch_probe WHERE consumer='fixture-late'"));
        Assert.Equal(1, await Count(source, "SELECT count(*) FROM processing.inbox_dedupe WHERE consumer='fixture-original'"));
    }

    [Fact]
    public async Task A_locked_event_is_skipped_and_an_unlocked_event_can_commit()
    {
        await using var source = await PrepareDatabase();
        var locked = await SeedEvent(source);
        var unlocked = await SeedEvent(source);
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await using (var hold = new NpgsqlCommand("SELECT id FROM processing.outbox WHERE id=@id FOR UPDATE", connection, transaction))
        {
            hold.Parameters.AddWithValue("id", locked.EventId);
            Assert.Equal(locked.EventId, await hold.ExecuteScalarAsync(Ct));
        }
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var dispatcher = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]);
        Assert.True(await dispatcher.DispatchOneAsync(timeout.Token));
        Assert.Null((await ReadState(source, locked.EventId)).DeliveredAt);
        Assert.NotNull((await ReadState(source, unlocked.EventId)).DeliveredAt);
        await AssertCounts(source, effects: 1, inboxes: 1);
        await transaction.RollbackAsync(Ct);
        Assert.True(await dispatcher.DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 2, inboxes: 2);
    }

    [Fact]
    public async Task Concurrent_dispatchers_claim_one_event_for_one_committed_attempt()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        var first = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]);
        var second = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var results = await Task.WhenAll(first.DispatchOneAsync(timeout.Token), second.DispatchOneAsync(timeout.Token));
        Assert.Equal(1, results.Count(value => value));
        await AssertCounts(source, effects: 1, inboxes: 1);
        var state = await ReadState(source, change.EventId);
        Assert.Equal(1, state.Attempts);
        Assert.NotNull(state.DeliveredAt);
    }

    [Fact]
    public async Task Retry_delay_is_capped_and_attempt_budget_dead_letters_without_ack()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        // Fixture-only operator action avoids sleeping through nine real retries.
        await using (var connection = await source.OpenConnectionAsync(Ct))
        await using (var setAttempt = new NpgsqlCommand("UPDATE processing.outbox SET attempts=9 WHERE id=@id", connection))
        {
            setAttempt.Parameters.AddWithValue("id", change.EventId);
            await setAttempt.ExecuteNonQueryAsync(Ct);
        }
        var dispatcher = new PostgresOutboxDispatcher(source, [Probe("fixture-consumer", fail: true)], maxAttempts: 11);
        var before = await DatabaseNow(source);
        Assert.True(await dispatcher.DispatchOneAsync(Ct));
        var capped = await ReadState(source, change.EventId);
        Assert.Equal(10, capped.Attempts);
        Assert.Null(capped.DeadAt);
        Assert.True(capped.AvailableAt >= before.AddSeconds(300));
        Assert.True(capped.AvailableAt <= capped.DatabaseNow.AddSeconds(300));
        await ForceDue(source, change.EventId);
        Assert.True(await dispatcher.DispatchOneAsync(Ct));
        var dead = await ReadState(source, change.EventId);
        Assert.Equal(11, dead.Attempts);
        Assert.NotNull(dead.DeadAt);
        Assert.Null(dead.DeliveredAt);
        Assert.DoesNotContain(PrivateMarker, dead.LastError ?? "");
        await ForceDue(source, change.EventId);
        Assert.False(await dispatcher.DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 0, inboxes: 0);
    }

    [Theory]
    [InlineData("payload")]
    [InlineData("workspace")]
    [InlineData("event-id")]
    [InlineData("event-type")]
    [InlineData("aggregate-version")]
    [InlineData("schema-version")]
    public async Task Invalid_or_mismatched_payload_is_rejected_before_consumer_effects(string mismatch)
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        await using (var connection = await source.OpenConnectionAsync(Ct))
        {
            var sql = mismatch switch
            {
                "event-type" => "UPDATE processing.outbox SET event_type='ObservationCorrected' WHERE id=@id",
                "aggregate-version" => "UPDATE processing.outbox SET aggregate_version=2 WHERE id=@id",
                "schema-version" => "UPDATE processing.outbox SET schema_version=2 WHERE id=@id",
                _ => "UPDATE processing.outbox SET payload=@payload WHERE id=@id"
            };
            await using var corrupt = new NpgsqlCommand(sql, connection);
            corrupt.Parameters.AddWithValue("id", change.EventId);
            if (mismatch is not ("event-type" or "aggregate-version" or "schema-version"))
            {
                var payload = mismatch switch
                {
                    "workspace" => DomainChangeV1Json.Serialize(change with { Workspace = OtherWorkspace, Subject = change.Subject with { Workspace = OtherWorkspace } }),
                    "event-id" => DomainChangeV1Json.Serialize(change with { EventId = Guid.NewGuid() }),
                    _ => "{\"unexpected\":\"" + PrivateMarker + "\"}"
                };
                corrupt.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
            }
            await corrupt.ExecuteNonQueryAsync(Ct);
        }
        Assert.True(await new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")], maxAttempts: 1).DispatchOneAsync(Ct));
        var state = await ReadState(source, change.EventId);
        Assert.Equal(1, state.Attempts);
        Assert.NotNull(state.DeadAt);
        Assert.Null(state.DeliveredAt);
        Assert.False(string.IsNullOrWhiteSpace(state.LastError));
        Assert.DoesNotContain(PrivateMarker, state.LastError!);
        await AssertCounts(source, effects: 0, inboxes: 0);
    }

    [Fact]
    public async Task Cancellation_after_a_handler_write_rolls_back_and_propagates_without_retry_metadata()
    {
        await using var source = await PrepareDatabase();
        var change = await SeedEvent(source);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var interrupted = new SqlConsumer("fixture-consumer", async (evt, connection, transaction, ct) =>
        {
            await InsertEffect("fixture-consumer", evt, connection, transaction, ct);
            cancel.Cancel();
            ct.ThrowIfCancellationRequested();
        });
        var dispatcher = new PostgresOutboxDispatcher(source, [interrupted]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatcher.DispatchOneAsync(cancel.Token));
        await AssertCounts(source, effects: 0, inboxes: 0);
        var state = await ReadState(source, change.EventId);
        Assert.Equal(0, state.Attempts);
        Assert.Null(state.DeliveredAt);
        Assert.Null(state.DeadAt);
        Assert.Null(state.LastError);
        Assert.True(await new PostgresOutboxDispatcher(source, [Probe("fixture-consumer")]).DispatchOneAsync(Ct));
        await AssertCounts(source, effects: 1, inboxes: 1);
    }

    private async Task<NpgsqlDataSource> PrepareDatabase()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        var source = NpgsqlDataSource.Create(connectionString);
        try
        {
            await using var connection = await source.OpenConnectionAsync(Ct);
            await using var setup = new NpgsqlCommand("""
                CREATE TABLE processing.dispatch_aggregates(id uuid PRIMARY KEY, workspace_id uuid NOT NULL, version bigint NOT NULL);
                CREATE TABLE processing.dispatch_probe(consumer text NOT NULL, event_id uuid NOT NULL, workspace_id uuid NOT NULL, correlation_id uuid NOT NULL);
                INSERT INTO registry.workspaces(id,display_name) VALUES
                  ('00000000-0000-0000-0000-000000000021','synthetic-dispatch-A'),
                  ('00000000-0000-0000-0000-000000000022','synthetic-dispatch-B');
                """, connection);
            await setup.ExecuteNonQueryAsync(Ct);
            return source;
        }
        catch { await source.DisposeAsync(); throw; }
    }

    private static async Task<DomainChangeV1> SeedEvent(NpgsqlDataSource source)
    {
        var aggregate = Guid.NewGuid();
        var change = new DomainChangeV1(Guid.NewGuid(), "ObservationRecorded", 1, Workspace,
            "observation-fixture-" + aggregate.ToString("N"), 1,
            new SubjectRef(Workspace, "Winery", "WineBatch", "batch-fixture-A"), At, At, At, Guid.NewGuid(),
            ["wine_temperature"], "observations:synthetic-fixture", SourceVersion: new SourceVersionRef("manual-fixture", "1"));
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await using (var canonical = new NpgsqlCommand("INSERT INTO processing.dispatch_aggregates(id,workspace_id,version) VALUES (@id,@workspace,1)", connection, transaction))
        {
            canonical.Parameters.AddWithValue("id", aggregate);
            canonical.Parameters.AddWithValue("workspace", Workspace.Value);
            await canonical.ExecuteNonQueryAsync(Ct);
        }
        await OutboxWriter.AppendAsync(connection, transaction, new(Workspace, "Observations", "Measurement", aggregate), change, Ct);
        await transaction.CommitAsync(Ct);
        return change;
    }

    private static IOutboxConsumer Probe(string name, bool fail = false, bool sqlFailure = false) => new SqlConsumer(name, async (change, connection, transaction, ct) =>
    {
        await InsertEffect(name, change, connection, transaction, ct);
        if (sqlFailure)
        {
            // Real SQL failure aborts the transaction until dispatcher savepoint rollback.
            await using var invalid = new NpgsqlCommand("SELECT 1/0", connection, transaction);
            await invalid.ExecuteNonQueryAsync(ct);
        }
        if (fail) throw new InvalidOperationException(PrivateMarker);
    });

    private static async Task InsertEffect(string consumer, DomainChangeV1 change, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("INSERT INTO processing.dispatch_probe(consumer,event_id,workspace_id,correlation_id) VALUES (@consumer,@event,@workspace,@correlation)", connection, transaction);
        command.Parameters.AddWithValue("consumer", consumer);
        command.Parameters.AddWithValue("event", change.EventId);
        command.Parameters.AddWithValue("workspace", change.Workspace.Value);
        command.Parameters.AddWithValue("correlation", change.CorrelationId);
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task ForceDue(NpgsqlDataSource source, Guid eventId, bool resetDelivered = false)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        var sql = resetDelivered
            ? "UPDATE processing.outbox SET available_at=clock_timestamp()-interval '1 second',delivered_at=NULL WHERE id=@id"
            : "UPDATE processing.outbox SET available_at=clock_timestamp()-interval '1 second' WHERE id=@id";
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", eventId);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static async Task AssertCounts(NpgsqlDataSource source, long effects, long inboxes)
    {
        Assert.Equal(effects, await Count(source, "SELECT count(*) FROM processing.dispatch_probe"));
        Assert.Equal(inboxes, await Count(source, "SELECT count(*) FROM processing.inbox_dedupe"));
    }

    private static async Task<long> Count(NpgsqlDataSource source, string sql)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        return (long)(await command.ExecuteScalarAsync(Ct) ?? throw new InvalidOperationException("Fixture count missing."));
    }

    private static async Task<DateTimeOffset> DatabaseNow(NpgsqlDataSource source)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT clock_timestamp()", connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    private static async Task<DeliveryState> ReadState(NpgsqlDataSource source, Guid eventId)
    {
        await using var connection = await source.OpenConnectionAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT attempts,available_at,delivered_at,dead_at,last_error,clock_timestamp() FROM processing.outbox WHERE id=@id", connection);
        command.Parameters.AddWithValue("id", eventId);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        return new(reader.GetInt32(0), reader.GetFieldValue<DateTimeOffset>(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.IsDBNull(3) ? null : reader.GetFieldValue<DateTimeOffset>(3),
            reader.IsDBNull(4) ? null : reader.GetString(4), reader.GetFieldValue<DateTimeOffset>(5));
    }

    private sealed record DeliveryState(int Attempts, DateTimeOffset AvailableAt, DateTimeOffset? DeliveredAt,
        DateTimeOffset? DeadAt, string? LastError, DateTimeOffset DatabaseNow);

    private sealed class SqlConsumer(string name, Func<DomainChangeV1, NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task> handler) : IOutboxConsumer
    {
        public string Name { get; } = name;
        public Task HandleAsync(DomainChangeV1 change, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken ct)
            => handler(change, connection, transaction, ct);
    }
}
