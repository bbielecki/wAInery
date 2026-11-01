using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Text.RegularExpressions;

namespace wAInery.BuildingBlocks;

public static class DomainChangeV1Json
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        Converters = { new UtcTimestampConverter() }
    };
    private static readonly HashSet<string> EventTypes = new(StringComparer.Ordinal)
    {
        "VineyardProfileChanged", "PhenologicalStageConfirmed", "VineyardActivityRecorded",
        "WineBatchStateChanged", "WineryOperationRecorded", "VesselOccupancyChanged",
        "ObservationRecorded", "ObservationCorrected", "WeatherForecastPublished",
        "KnowledgeIndexPublished", "DocumentRightsChanged", "PredictionInvalidated",
        "PredictionUpdated", "DecisionPlanPublished", "DecisionPlanSuperseded"
    };
    private static readonly HashSet<string> Modules = new(StringComparer.Ordinal)
        { "Vineyard", "Winery", "Observations", "Weather", "Knowledge", "Connectivity", "Decisions" };
    private static readonly HashSet<string> Classifications = new(StringComparer.Ordinal)
        { "PRIVATE_COPYRIGHT", "PRIVATE_DERIVED", "PRIVATE_OPERATIONAL", "PUBLIC_LICENSED", "PUBLIC_REFERENCE" };

    public static DomainChangeV1 Deserialize(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            RejectNullValues(document.RootElement);
            var wire = document.RootElement.Deserialize<WireEnvelope>(Options)
                ?? throw new JsonException("Domain change must be an object.");
            var workspace = new WorkspaceId(wire.WorkspaceId);
            var change = new DomainChangeV1(wire.EventId, wire.EventType, wire.SchemaVersion,
                workspace, wire.AggregateId, wire.AggregateVersion,
                new SubjectRef(workspace, wire.Subject.Module, wire.Subject.Kind, wire.Subject.Id),
                wire.EffectiveAt.ToUniversalTime(), wire.OccurredAt.ToUniversalTime(), wire.RecordedAt.ToUniversalTime(),
                wire.CorrelationId, Array.AsReadOnly(wire.ChangedFields), wire.DataRef,
                wire.CausationId, wire.SourceVersion,
                wire.TimeRange is null ? null : new TimeWindow(wire.TimeRange.From.ToUniversalTime(), wire.TimeRange.To.ToUniversalTime()));
            Validate(change);
            return change;
        }
        catch (ArgumentException error)
        {
            throw new JsonException("Invalid domain change envelope.", error);
        }
    }

    // Every field in the v1 event schema is non-nullable when present, including
    // optional metadata. Nullable CLR values represent absence on the wire only.
    private static void RejectNullValues(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
            throw new JsonException("Null is not a v1 event field value; omit optional metadata instead.");
        if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) RejectNullValues(property.Value);
        if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectNullValues(item);
    }

    public static string Serialize(DomainChangeV1 change)
    {
        Validate(change);
        return JsonSerializer.Serialize(new WireEnvelope
        {
            EventId = change.EventId, EventType = change.EventType, SchemaVersion = change.SchemaVersion,
            WorkspaceId = change.Workspace.Value, AggregateId = change.AggregateId,
            AggregateVersion = change.AggregateVersion,
            Subject = new WireSubject { Module = change.Subject.Module, Kind = change.Subject.Kind, Id = change.Subject.Id },
            EffectiveAt = change.EffectiveAt.ToUniversalTime(), OccurredAt = change.OccurredAt.ToUniversalTime(),
            RecordedAt = change.RecordedAt.ToUniversalTime(), CorrelationId = change.CorrelationId,
            ChangedFields = change.ChangedFields.ToArray(), DataRef = change.DataRef,
            CausationId = change.CausationId, SourceVersion = change.SourceVersion,
            TimeRange = change.TimeRange is { } range
                ? new WireRange { From = range.From.ToUniversalTime(), To = range.To.ToUniversalTime() } : null
        }, Options);
    }

    private static void Validate(DomainChangeV1 change)
    {
        ArgumentNullException.ThrowIfNull(change);
        if (change.EventId == Guid.Empty || change.CorrelationId == Guid.Empty || change.CausationId == Guid.Empty
            || change.SchemaVersion != 1 || change.Workspace.Value == Guid.Empty
            || !EventTypes.Contains(change.EventType) || change.AggregateVersion < 1
            || string.IsNullOrWhiteSpace(change.AggregateId) || string.IsNullOrWhiteSpace(change.DataRef)
            || change.Subject is null || !change.Subject.HasValidScope || change.Subject.Workspace != change.Workspace
            || !Modules.Contains(change.Subject.Module) || change.Subject.Id.Length > 128 || change.Subject.Kind.Length > 128
            || change.ChangedFields is null || change.ChangedFields.Count == 0
            || change.ChangedFields.Any(string.IsNullOrWhiteSpace)
            || change.ChangedFields.Distinct(StringComparer.Ordinal).Count() != change.ChangedFields.Count)
            throw new ArgumentException("Invalid domain change identity, scope, version or changed fields.");
        if (change.SourceVersion is { } source && (string.IsNullOrWhiteSpace(source.SourceId)
            || string.IsNullOrWhiteSpace(source.Version)
            || (source.Classification is not null && !Classifications.Contains(source.Classification))))
            throw new ArgumentException("Invalid source version metadata.");
        if (change.TimeRange is { } range && range.To <= range.From)
            throw new ArgumentException("Invalid effective time range.");
    }

    private sealed class WireEnvelope
    {
        public WireEnvelope() { }
        public required Guid EventId { get; init; }
        public required string EventType { get; init; }
        public required int SchemaVersion { get; init; }
        public required Guid WorkspaceId { get; init; }
        public required string AggregateId { get; init; }
        public required long AggregateVersion { get; init; }
        public required WireSubject Subject { get; init; }
        public required DateTimeOffset EffectiveAt { get; init; }
        public required DateTimeOffset OccurredAt { get; init; }
        public required DateTimeOffset RecordedAt { get; init; }
        public required Guid CorrelationId { get; init; }
        public required string[] ChangedFields { get; init; }
        public required string DataRef { get; init; }
        public Guid? CausationId { get; init; }
        public SourceVersionRef? SourceVersion { get; init; }
        public WireRange? TimeRange { get; init; }
    }

    private sealed class WireSubject
    {
        public WireSubject() { }
        public required string Module { get; init; }
        public required string Kind { get; init; }
        public required string Id { get; init; }
    }

    private sealed class WireRange
    {
        public WireRange() { }
        public required DateTimeOffset From { get; init; }
        public required DateTimeOffset To { get; init; }
    }

    private sealed class UtcTimestampConverter : JsonConverter<DateTimeOffset>
    {
        private static readonly Regex Pattern = new(
            @"^[0-9]{4}-[0-9]{2}-[0-9]{2}[Tt][0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]+)?(?:[Zz]|[+-][0-9]{2}:[0-9]{2})$",
            RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            if (value is null || !Pattern.IsMatch(value)
                || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                throw new JsonException("Event timestamps require RFC3339 date-time with explicit offset.");
            return timestamp.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToUniversalTime());
    }
}
