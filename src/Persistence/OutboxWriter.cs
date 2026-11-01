using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using wAInery.BuildingBlocks;

namespace wAInery.Persistence;

public sealed record OutboxMetadata(WorkspaceId Workspace, string Producer, string AggregateKind, Guid InternalAggregateId);

// Infrastructure only. Canonical writes/idempotency and transaction lifetime belong
// to the owner. An append exception must fail the entire accepted owner command.
public static class OutboxWriter
{
    private static readonly IReadOnlyDictionary<string, string> Owners = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["VineyardProfileChanged"] = "Vineyard", ["PhenologicalStageConfirmed"] = "Vineyard",
        ["VineyardActivityRecorded"] = "Vineyard", ["WineBatchStateChanged"] = "Winery",
        ["WineryOperationRecorded"] = "Winery", ["VesselOccupancyChanged"] = "Winery",
        ["ObservationRecorded"] = "Observations", ["ObservationCorrected"] = "Observations",
        ["WeatherForecastPublished"] = "Weather", ["KnowledgeIndexPublished"] = "Knowledge",
        ["DocumentRightsChanged"] = "Knowledge", ["PredictionInvalidated"] = "Decisions",
        ["PredictionUpdated"] = "Decisions", ["DecisionPlanPublished"] = "Decisions",
        ["DecisionPlanSuperseded"] = "Decisions"
    };

    public static async Task AppendAsync(NpgsqlConnection connection, NpgsqlTransaction transaction,
        OutboxMetadata metadata, DomainChangeV1 change, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(change);
        if (connection.State != ConnectionState.Open || !ReferenceEquals(transaction.Connection, connection))
            throw new ArgumentException("A matching active caller transaction is required.");
        if (metadata.Workspace.Value == Guid.Empty || metadata.Workspace != change.Workspace ||
            metadata.InternalAggregateId == Guid.Empty || string.IsNullOrWhiteSpace(metadata.AggregateKind) ||
            !Owners.TryGetValue(change.EventType, out var owner) || metadata.Producer != owner)
            throw new ArgumentException("Outbox canonical identity, scope or event owner mismatch.");

        // Decode our strict wire snapshot too: metadata columns and payload must
        // agree even if the caller supplied a mutable ChangedFields collection.
        var snapshot = DomainChangeV1Json.Deserialize(DomainChangeV1Json.Serialize(change));
        snapshot = snapshot with
        {
            EffectiveAt = PersistedTimestamp(snapshot.EffectiveAt),
            OccurredAt = PersistedTimestamp(snapshot.OccurredAt),
            RecordedAt = PersistedTimestamp(snapshot.RecordedAt)
        };
        var payload = DomainChangeV1Json.Serialize(snapshot);
        using var document = JsonDocument.Parse(payload);
        await using var insert = new NpgsqlCommand("""
            INSERT INTO processing.outbox
              (id,workspace_id,producer,aggregate_kind,aggregate_id,aggregate_version,
               event_type,schema_version,subject,changed_fields,data_ref,payload,
               effective_at,occurred_at,recorded_at,correlation_id,causation_id)
            VALUES
              (@id,@workspace,@producer,@kind,@aggregate,@version,@type,@schema,
               @subject,@fields,@ref,@payload,@effective,@occurred,@recorded,@correlation,@causation)
            """, connection, transaction) { CommandTimeout = 30 };
        insert.Parameters.AddWithValue("id", snapshot.EventId);
        insert.Parameters.AddWithValue("workspace", metadata.Workspace.Value);
        insert.Parameters.AddWithValue("producer", metadata.Producer);
        insert.Parameters.AddWithValue("kind", metadata.AggregateKind);
        insert.Parameters.AddWithValue("aggregate", metadata.InternalAggregateId);
        insert.Parameters.AddWithValue("version", snapshot.AggregateVersion);
        insert.Parameters.AddWithValue("type", snapshot.EventType);
        insert.Parameters.AddWithValue("schema", snapshot.SchemaVersion);
        insert.Parameters.AddWithValue("subject", NpgsqlDbType.Jsonb, document.RootElement.GetProperty("subject").GetRawText());
        insert.Parameters.AddWithValue("fields", snapshot.ChangedFields.ToArray());
        insert.Parameters.AddWithValue("ref", snapshot.DataRef);
        insert.Parameters.AddWithValue("payload", NpgsqlDbType.Jsonb, payload);
        insert.Parameters.AddWithValue("effective", snapshot.EffectiveAt);
        insert.Parameters.AddWithValue("occurred", snapshot.OccurredAt);
        insert.Parameters.AddWithValue("recorded", snapshot.RecordedAt);
        insert.Parameters.AddWithValue("correlation", snapshot.CorrelationId);
        insert.Parameters.Add(new NpgsqlParameter("causation", NpgsqlDbType.Uuid) { Value = (object?)snapshot.CausationId ?? DBNull.Value });
        await insert.ExecuteNonQueryAsync(ct);
    }

    private static DateTimeOffset PersistedTimestamp(DateTimeOffset value)
    {
        var ticks = value.UtcTicks - value.UtcTicks % 10;
        if (ticks == 0 || value == DateTimeOffset.MaxValue)
            throw new ArgumentException("A finite event timestamp is required.");
        // PostgreSQL stores microseconds; quantize before both JSON and columns.
        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }
}
