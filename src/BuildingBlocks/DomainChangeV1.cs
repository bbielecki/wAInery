namespace wAInery.BuildingBlocks;

// In-process scope is explicit. The v1 wire codec keeps workspaceId at the root.
public sealed record DomainChangeV1(
    Guid EventId, string EventType, int SchemaVersion, WorkspaceId Workspace,
    string AggregateId, long AggregateVersion, SubjectRef Subject,
    DateTimeOffset EffectiveAt, DateTimeOffset OccurredAt, DateTimeOffset RecordedAt,
    Guid CorrelationId, IReadOnlyList<string> ChangedFields, string DataRef,
    Guid? CausationId = null, SourceVersionRef? SourceVersion = null, TimeWindow? TimeRange = null);

public sealed record SourceVersionRef(
    string SourceId, string Version, string? Checksum = null, string? Classification = null);
