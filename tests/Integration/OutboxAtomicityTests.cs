using System.Text.Json;
using Npgsql;
using wAInery.BuildingBlocks;
using wAInery.Persistence;

namespace wAInery.Integration.Tests;

// The probe is a fixture aggregate, not a Vineyard/Winery command implementation.
public sealed class OutboxAtomicityTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly WorkspaceId Workspace = new(Guid.Parse("00000000-0000-0000-0000-000000000011"));
    private static readonly Guid AggregateId = Guid.Parse("70000000-0000-0000-0000-000000000011");
    private static readonly DateTimeOffset At = new(2026, 4, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Canonical_write_and_outbox_are_invisible_until_the_caller_commits()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await InsertCanonical(connection, transaction);
        await OutboxWriter.AppendAsync(connection, transaction, Metadata(), Change(), Ct);
        await AssertCounts(connectionString, 0, 0);
        await transaction.CommitAsync(Ct);
        await AssertCounts(connectionString, 1, 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rollback_before_or_after_append_leaves_neither_record(bool appendBeforeRollback)
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await InsertCanonical(connection, transaction);
        if (appendBeforeRollback)
            await OutboxWriter.AppendAsync(connection, transaction, Metadata(), Change(), Ct);
        await transaction.RollbackAsync(Ct);
        await AssertCounts(connectionString, 0, 0);
    }

    [Fact]
    public async Task Disposing_an_uncommitted_transaction_rolls_back_canonical_and_event()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await InsertCanonical(connection, transaction);
            await OutboxWriter.AppendAsync(connection, transaction, Metadata(), Change(), Ct);
        }
        await AssertCounts(connectionString, 0, 0);
    }

    [Fact]
    public async Task Missing_caller_transaction_is_rejected_without_starting_an_implicit_transaction()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            OutboxWriter.AppendAsync(connection, null!, Metadata(), Change(), Ct));
        await AssertCounts(connectionString, 0, 0);
    }

    [Fact]
    public async Task A_completed_transaction_cannot_publish_an_event()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await transaction.CommitAsync(Ct);
        await AssertTransactionRejected(() => OutboxWriter.AppendAsync(connection, transaction, Metadata(), Change(), Ct));
        await AssertCounts(connectionString, 0, 0);
    }

    [Fact]
    public async Task A_transaction_from_another_connection_is_rejected_without_settling_the_caller_transaction()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await using var other = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await other.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await InsertCanonical(connection, transaction);
        await AssertTransactionRejected(() => OutboxWriter.AppendAsync(other, transaction, Metadata(), Change(), Ct));
        await OutboxWriter.AppendAsync(connection, transaction, Metadata(), Change(), Ct);
        await AssertCounts(connectionString, 0, 0);
        await transaction.RollbackAsync(Ct);
        await AssertCounts(connectionString, 0, 0);
    }

    [Theory]
    [InlineData("metadata-workspace")]
    [InlineData("canonical-identity")]
    [InlineData("wire-workspace")]
    [InlineData("event-producer")]
    public async Task Invalid_scope_or_canonical_identity_is_rejected_before_publication(string invalidPart)
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var transaction = await connection.BeginTransactionAsync(Ct);
        await InsertCanonical(connection, transaction);
        var metadata = Metadata();
        var change = Change();
        var otherWorkspace = new WorkspaceId(Guid.Parse("00000000-0000-0000-0000-000000000012"));
        if (invalidPart == "metadata-workspace") metadata = metadata with { Workspace = otherWorkspace };
        if (invalidPart == "canonical-identity") metadata = metadata with { InternalAggregateId = Guid.Empty };
        if (invalidPart == "event-producer") metadata = metadata with { Producer = "Vineyard" };
        if (invalidPart == "wire-workspace") change = change with { Subject = change.Subject with { Workspace = otherWorkspace } };
        await Assert.ThrowsAnyAsync<ArgumentException>(() => OutboxWriter.AppendAsync(connection, transaction, metadata, change, Ct));
        await transaction.RollbackAsync(Ct);
        await AssertCounts(connectionString, 0, 0);
    }

    [Fact]
    public async Task Duplicate_logical_version_aborts_the_canonical_update_instead_of_silently_ignoring_the_event()
    {
        var connectionString = await CreateDatabase();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using (var first = await connection.BeginTransactionAsync(Ct))
        {
            await InsertCanonical(connection, first);
            await OutboxWriter.AppendAsync(connection, first, Metadata(), Change(), Ct);
            await first.CommitAsync(Ct);
        }
        await using (var retry = await connection.BeginTransactionAsync(Ct))
        {
            await using var update = new NpgsqlCommand("UPDATE processing.atomicity_probe SET value=2 WHERE id=@id", connection, retry);
            update.Parameters.AddWithValue("id", AggregateId);
            Assert.Equal(1, await update.ExecuteNonQueryAsync(Ct));
            var duplicate = Change() with { EventId = Guid.Parse("80000000-0000-0000-0000-000000000012") };
            var error = await Assert.ThrowsAsync<PostgresException>(() => OutboxWriter.AppendAsync(connection, retry, Metadata(), duplicate, Ct));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, error.SqlState);
            await retry.RollbackAsync(Ct);
        }
        await AssertCounts(connectionString, 1, 1);
        await using var original = new NpgsqlCommand("SELECT value FROM processing.atomicity_probe WHERE id=@id", connection);
        original.Parameters.AddWithValue("id", AggregateId);
        Assert.Equal(1, await original.ExecuteScalarAsync(Ct));
    }

    [Fact]
    public async Task Payload_roundtrips_strict_v1_with_opaque_ids_and_parameterized_metadata()
    {
        var connectionString = await CreateDatabase();
        var change = Change() with
        {
            CausationId = Guid.Parse("90000000-0000-0000-0000-000000000012"),
            SourceVersion = new SourceVersionRef("manual-fixture", "revision-1", "synthetic-checksum", "PRIVATE_OPERATIONAL"),
            TimeRange = new TimeWindow(At, At.AddHours(1)),
            DataRef = "observations:fixture'); DROP TABLE processing.atomicity_probe; --"
        };
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await InsertCanonical(connection, transaction);
            await OutboxWriter.AppendAsync(connection, transaction, Metadata(), change, Ct);
            await transaction.CommitAsync(Ct);
        }
        await AssertCounts(connectionString, 1, 1);
        await using var command = new NpgsqlCommand("""
            SELECT id,workspace_id,producer,aggregate_kind,aggregate_id,aggregate_version,
                   subject::text,changed_fields,data_ref,payload::text,effective_at,occurred_at,
                   recorded_at,correlation_id,causation_id,schema_version
            FROM processing.outbox
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        Assert.Equal(change.EventId, reader.GetGuid(0));
        Assert.Equal(Workspace.Value, reader.GetGuid(1));
        Assert.Equal("Observations", reader.GetString(2));
        Assert.Equal("Measurement", reader.GetString(3));
        Assert.Equal(AggregateId, reader.GetGuid(4));
        Assert.Equal(change.AggregateVersion, reader.GetInt64(5));
        using var subject = JsonDocument.Parse(reader.GetString(6));
        Assert.Equal(change.Subject.Id, subject.RootElement.GetProperty("id").GetString());
        Assert.Equal(change.Subject.Module, subject.RootElement.GetProperty("module").GetString());
        Assert.Equal(change.Subject.Kind, subject.RootElement.GetProperty("kind").GetString());
        Assert.False(subject.RootElement.TryGetProperty("workspaceId", out _));
        Assert.False(subject.RootElement.TryGetProperty("workspace", out _));
        Assert.Equal(change.ChangedFields.ToArray(), reader.GetFieldValue<string[]>(7));
        Assert.Equal(change.DataRef, reader.GetString(8));
        var payload = reader.GetString(9);
        var consumed = DomainChangeV1Json.Deserialize(payload);
        Assert.Equal(change.EventId, consumed.EventId);
        Assert.Equal(change.EventType, consumed.EventType);
        Assert.Equal(change.SchemaVersion, consumed.SchemaVersion);
        Assert.Equal(change.Workspace, consumed.Workspace);
        Assert.Equal(change.AggregateVersion, consumed.AggregateVersion);
        Assert.Equal(change.EffectiveAt, consumed.EffectiveAt);
        Assert.Equal(change.OccurredAt, consumed.OccurredAt);
        Assert.Equal(change.RecordedAt, consumed.RecordedAt);
        Assert.Equal(change.CorrelationId, consumed.CorrelationId);
        Assert.Equal(change.ChangedFields, consumed.ChangedFields);
        Assert.Equal(change.AggregateId, consumed.AggregateId);
        Assert.Equal(change.Subject, consumed.Subject);
        Assert.Equal(change.CausationId, consumed.CausationId);
        Assert.Equal(change.SourceVersion, consumed.SourceVersion);
        Assert.Equal(change.TimeRange, consumed.TimeRange);
        Assert.Equal(change.DataRef, consumed.DataRef);
        Assert.Equal(change.EffectiveAt, reader.GetFieldValue<DateTimeOffset>(10));
        Assert.Equal(change.OccurredAt, reader.GetFieldValue<DateTimeOffset>(11));
        Assert.Equal(change.RecordedAt, reader.GetFieldValue<DateTimeOffset>(12));
        Assert.Equal(change.CorrelationId, reader.GetGuid(13));
        Assert.Equal(change.CausationId, reader.GetGuid(14));
        Assert.Equal(change.SchemaVersion, reader.GetInt32(15));
        Assert.False(await reader.ReadAsync(Ct));
    }

    [Fact]
    public async Task Event_timestamp_columns_and_payload_share_PostgreSQL_microsecond_precision()
    {
        var connectionString = await CreateDatabase();
        var change = Change() with
        {
            EffectiveAt = At.AddTicks(17),
            OccurredAt = At.AddTicks(27),
            RecordedAt = At.AddTicks(37),
            TimeRange = new TimeWindow(At.AddTicks(47), At.AddHours(1).AddTicks(57))
        };
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using (var transaction = await connection.BeginTransactionAsync(Ct))
        {
            await InsertCanonical(connection, transaction);
            await OutboxWriter.AppendAsync(connection, transaction, Metadata(), change, Ct);
            await transaction.CommitAsync(Ct);
        }
        await using var command = new NpgsqlCommand("""
            SELECT payload::text,effective_at,occurred_at,recorded_at
            FROM processing.outbox
            """, connection);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        var consumed = DomainChangeV1Json.Deserialize(reader.GetString(0));
        Assert.Equal(At.AddTicks(10), consumed.EffectiveAt);
        Assert.Equal(At.AddTicks(20), consumed.OccurredAt);
        Assert.Equal(At.AddTicks(30), consumed.RecordedAt);
        Assert.Equal(reader.GetFieldValue<DateTimeOffset>(1), consumed.EffectiveAt);
        Assert.Equal(reader.GetFieldValue<DateTimeOffset>(2), consumed.OccurredAt);
        Assert.Equal(reader.GetFieldValue<DateTimeOffset>(3), consumed.RecordedAt);
        Assert.Equal(change.TimeRange, consumed.TimeRange);
        Assert.False(await reader.ReadAsync(Ct));
    }

    private async Task<string> CreateDatabase()
    {
        var connectionString = await fixture.CreateDatabaseAsync(Ct);
        await new MigrationRunner().ApplyAsync(connectionString, Ct);
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(Ct);
        await using var setup = new NpgsqlCommand("""
            CREATE TABLE processing.atomicity_probe(id uuid PRIMARY KEY, value integer NOT NULL);
            INSERT INTO registry.workspaces(id,display_name) VALUES (@workspace,'synthetic-outbox-fixture');
            """, connection);
        setup.Parameters.AddWithValue("workspace", Workspace.Value);
        await setup.ExecuteNonQueryAsync(Ct);
        return connectionString;
    }

    private static OutboxMetadata Metadata() => new(Workspace, "Observations", "Measurement", AggregateId);

    private static DomainChangeV1 Change() => new(
        Guid.Parse("80000000-0000-0000-0000-000000000011"), "ObservationRecorded", 1, Workspace,
        "obs-synthetic-opaque", 1, new SubjectRef(Workspace, "Winery", "WineBatch", "wb-synthetic-opaque"),
        At, At, At, Guid.Parse("90000000-0000-0000-0000-000000000011"),
        ["wine_temperature"], "observations:obs-synthetic-opaque", SourceVersion: new SourceVersionRef("manual-fixture", "1"));

    private static async Task InsertCanonical(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            "INSERT INTO processing.atomicity_probe(id,value) VALUES (@id,1)", connection, transaction);
        command.Parameters.AddWithValue("id", AggregateId);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static async Task AssertCounts(string connectionString, long canonical, long events)
    {
        await using var observer = new NpgsqlConnection(connectionString);
        await observer.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("""
            SELECT (SELECT count(*) FROM processing.atomicity_probe),
                   (SELECT count(*) FROM processing.outbox)
            """, observer);
        await using var reader = await command.ExecuteReaderAsync(Ct);
        Assert.True(await reader.ReadAsync(Ct));
        Assert.Equal(canonical, reader.GetInt64(0));
        Assert.Equal(events, reader.GetInt64(1));
    }

    private static async Task AssertTransactionRejected(Func<Task> action)
    {
        var error = await Record.ExceptionAsync(action);
        Assert.True(error is ArgumentException or InvalidOperationException,
            "A nonmatching or inactive caller transaction must be rejected explicitly.");
    }
}
