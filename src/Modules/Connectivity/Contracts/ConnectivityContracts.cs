using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Connectivity.Contracts;

public enum SourceStatus { Active, Degraded, Disabled }
public enum ReceiptStatus { Received, Parsed, Proposed, Accepted, Rejected, Failed }
public enum ImportRunStatus { Pending, Running, Succeeded, Failed }
public enum DraftTarget { Observation, VineyardActivityProposal, CellarOperationProposal, ForecastRun, DocumentVersion }
public sealed record SourceCapabilitiesDto(bool Manual, bool Poll, ImmutableArray<DraftTarget> Targets,
    ImmutableArray<string> ExpectedMeasurementTypes, string PayloadSchemaVersion, bool UsesSecretReference);
public sealed record SourceSnapshotDto(WorkspaceId Workspace, string SourceId, long Version, string SourceKind,
    string DisplayName, SourceStatus Status, string AdapterKind, string AdapterVersion, string MappingVersion,
    string ConfigurationRef, SourceCapabilitiesDto Capabilities, SourceRightsSnapshot Rights, DateTimeOffset AsOf, DateTimeOffset KnownAt);
public sealed record ResolvedSourceDto(ResolvedSubject Identity, SourceSnapshotDto Source);
public sealed record RegisterSourceCommand(CommandContext Context, string SourceId, string SourceKind,
    string DisplayName, string AdapterKind, string AdapterVersion, string MappingVersion, string ConfigurationRef,
    SourceRightsClass RightsClass, string? RightsBasisRef)
{
    public ExternalAiPolicy InitialPolicy => ExternalAiPolicy.Deny;
}
public sealed record UpdateSourceRightsCommand(CommandContext Context, string SourceId,
    SourceRightsClass RightsClass, ExternalAiPolicy RequestedPolicy, string BasisRef);
public sealed record SetSourceStatusCommand(CommandContext Context, string SourceId, SourceStatus Status, string ReasonCode);

public sealed record CaptureReceiptCommand
{
    public CommandContext Context { get; }
    public string SourceId { get; }
    public long SourceDefinitionVersion { get; }
    public string ExternalKey { get; }
    public StoredObjectRef Payload { get; }
    public string AdapterVersion { get; }
    public string MappingVersion { get; }
    public DateTimeOffset? SourcePublishedAt { get; }
    public string? SourceRecordVersion { get; }
    public string? SupersedesReceiptId { get; }
    public CaptureReceiptCommand(CommandContext context, string sourceId, long sourceDefinitionVersion,
        string externalKey, StoredObjectRef payload, string adapterVersion, string mappingVersion,
        DateTimeOffset? sourcePublishedAt = null, string? sourceRecordVersion = null, string? supersedesReceiptId = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(adapterVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(mappingVersion);
        if (sourceDefinitionVersion < 1 || context.Workspace != payload.Workspace)
            throw new ArgumentException("Receipt source version/object scope mismatch.");
        (Context, SourceId, SourceDefinitionVersion, ExternalKey, Payload, AdapterVersion, MappingVersion,
            SourcePublishedAt, SourceRecordVersion, SupersedesReceiptId) =
            (context, sourceId, sourceDefinitionVersion, externalKey, payload, adapterVersion, mappingVersion,
            sourcePublishedAt?.ToUniversalTime(), sourceRecordVersion, supersedesReceiptId);
    }
}
public sealed record SourceReceiptDto(WorkspaceId Workspace, string ReceiptId, string SourceId,
    long SourceDefinitionVersion, string ExternalKey, StoredObjectRef Payload, string AdapterVersion, string MappingVersion,
    DateTimeOffset? SourcePublishedAt, string? SourceRecordVersion, DateTimeOffset ReceivedAt, DateTimeOffset RecordedAt,
    ReceiptStatus Status, string? ReasonCode, string? SupersedesReceiptId, bool Duplicate);
public sealed record ProcessReceiptCommand(CommandContext Context, string ReceiptId, string AdapterVersion, string MappingVersion);
public sealed record ReplayImportCommand(CommandContext Context, string SourceId, string OriginalRunId, string ReasonCode);
public sealed record ImportRunDto(WorkspaceId Workspace, string RunId, string SourceId, long SourceDefinitionVersion,
    ImmutableArray<string> ReceiptIds, string AdapterVersion, string MappingVersion, string? OriginalRunId,
    ImportRunStatus Status, DateTimeOffset RecordedAt, DateTimeOffset? CompletedAt, string? ErrorCode);
public sealed record DraftPayloadRef(StoredObjectRef Object, string OwnerContractId, int OwnerContractVersion);
// Payload remains an untrusted candidate for an installed owner-contract allowlist.
public sealed record ImportDraftDto(WorkspaceId Workspace, string DraftId, string RunId, string ReceiptId,
    DraftTarget Target, SubjectRef? ProposedSubject, DateTimeOffset? ProposedEffectiveAt, DraftPayloadRef Payload,
    ImmutableArray<string> MissingFields, ImmutableArray<EvidenceRef> EvidenceRefs);
public sealed record DraftPageDto(WorkspaceId Workspace, string RunId, DateTimeOffset AsOf, DateTimeOffset KnownAt,
    ImmutableArray<ImportDraftDto> Items, string? NextCursor);

public interface ISourceRegistryReadPort
{
    Task<Result<ResolvedSourceDto>> ResolveAsync(WorkspaceId workspace, string sourceId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> GetSourceAsync(WorkspaceId workspace, string sourceId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<SourceRightsSnapshot>> GetCurrentRightsAsync(WorkspaceId workspace, string sourceId, CancellationToken ct);
}
public interface IConnectivityReadPort
{
    Task<Result<SourceReceiptDto>> GetReceiptAsync(WorkspaceId workspace, string receiptId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<ImportRunDto>> GetImportRunAsync(WorkspaceId workspace, string runId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<DraftPageDto>> GetDraftsAsync(WorkspaceId workspace, string runId, DateTimeOffset asOf, int maxCount, string? cursor, CancellationToken ct);
}
public interface IConnectivityCommandPort
{
    Task<Result<SourceSnapshotDto>> RegisterSourceAsync(RegisterSourceCommand command, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> UpdateSourceRightsAsync(UpdateSourceRightsCommand command, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> SetSourceStatusAsync(SetSourceStatusCommand command, CancellationToken ct);
    Task<Result<SourceReceiptDto>> CaptureReceiptAsync(CaptureReceiptCommand command, CancellationToken ct);
    Task<Result<ImportRunDto>> ProcessReceiptAsync(ProcessReceiptCommand command, CancellationToken ct);
    Task<Result<ImportRunDto>> ReplayImportAsync(ReplayImportCommand command, CancellationToken ct);
}
public sealed record SourceFetchRequest(SourceSnapshotDto Source, string? Cursor, int MaxRecords);
public sealed record FetchedRecordDto(string ExternalKey, StoredObjectRef Payload, DateTimeOffset RetrievedAt,
    DateTimeOffset? SourcePublishedAt, string? SourceRecordVersion, string? SupersedesReceiptId);
public sealed record SourceFetchPageDto(ImmutableArray<FetchedRecordDto> Items, string? ProposedNextCursor,
    DateTimeOffset RetrievedAt, bool NotModified);
public sealed record SourceParseRequest(SourceSnapshotDto Source, SourceReceiptDto Receipt, string RunId, string AdapterVersion, string MappingVersion);
public sealed record ParsedDraftBatchDto(WorkspaceId Workspace, string RunId, ImmutableArray<ImportDraftDto> Items);
public interface ISourceAdapter
{
    string AdapterKind { get; }
    string AdapterVersion { get; }
    SourceCapabilitiesDto Capabilities { get; }
    Task<Result<SourceFetchPageDto>> FetchAsync(SourceFetchRequest request, CancellationToken ct);
    Task<Result<ParsedDraftBatchDto>> ParseAsync(SourceParseRequest request, CancellationToken ct);
}
