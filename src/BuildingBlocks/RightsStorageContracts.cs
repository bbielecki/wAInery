namespace wAInery.BuildingBlocks;

public enum SourceRightsClass { Unknown, PrivateCopyright, PrivateDerived, PrivateOperational, PublicLicensed, PublicReference }
public enum ExternalAiPolicy { Deny, Review, Allow }

// Audit metadata, never a reusable authorization token. Recheck current owner policy before egress.
public sealed record SourceRightsSnapshot
{
    public WorkspaceId Workspace { get; }
    public string SourceId { get; }
    public long RegistryVersion { get; }
    public SourceRightsClass RightsClass { get; }
    public DateTimeOffset CheckedAt { get; }
    public ExternalAiPolicy Policy { get; }
    public string? AuthorizationRef { get; }
    public SourceRightsSnapshot(WorkspaceId workspace, string sourceId, long registryVersion,
        SourceRightsClass rightsClass, DateTimeOffset checkedAt, ExternalAiPolicy policy = ExternalAiPolicy.Deny,
        string? authorizationRef = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        if (workspace.Value == Guid.Empty || registryVersion < 1 || !Enum.IsDefined(rightsClass) || !Enum.IsDefined(policy))
            throw new ArgumentException("Invalid source rights metadata.");
        if (policy == ExternalAiPolicy.Allow && (rightsClass == SourceRightsClass.Unknown || string.IsNullOrWhiteSpace(authorizationRef)))
            throw new ArgumentException("Explicit source authorization provenance is required.");
        (Workspace, SourceId, RegistryVersion, RightsClass, CheckedAt, Policy, AuthorizationRef) =
            (workspace, sourceId, registryVersion, rightsClass, checkedAt.ToUniversalTime(), policy, authorizationRef);
    }
}

public sealed record StoredObjectRef
{
    public WorkspaceId Workspace { get; }
    public string Key { get; }
    public string Sha256 { get; }
    public long LengthBytes { get; }
    public string MediaType { get; }
    public SourceRightsClass Classification { get; }
    public StoredObjectRef(WorkspaceId workspace, string key, string sha256, long lengthBytes,
        string mediaType, SourceRightsClass classification)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        if (workspace.Value == Guid.Empty || lengthBytes < 0 || !Enum.IsDefined(classification) || !IsSha256(sha256))
            throw new ArgumentException("Invalid scoped object descriptor.");
        (Workspace, Key, Sha256, LengthBytes, MediaType, Classification) = (workspace, key, sha256, lengthBytes, mediaType, classification);
    }
    internal static bool IsSha256(string? value) => value is { Length: 64 } &&
        value.All(ch => (ch >= '0' && ch <= '9') || (ch >= 'a' && ch <= 'f'));
}

public sealed record ObjectWriteRequest
{
    public CommandContext Context { get; }
    public string MediaType { get; }
    public SourceRightsClass Classification { get; }
    public long MaxBytes { get; }
    public string? ExpectedSha256 { get; }
    public ObjectWriteRequest(CommandContext context, string mediaType, SourceRightsClass classification,
        long maxBytes, string? expectedSha256 = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(mediaType);
        if (maxBytes <= 0 || !Enum.IsDefined(classification) || (expectedSha256 is not null && !StoredObjectRef.IsSha256(expectedSha256)))
            throw new ArgumentException("Invalid bounded object write request.");
        (Context, MediaType, Classification, MaxBytes, ExpectedSha256) = (context, mediaType, classification, maxBytes, expectedSha256);
    }
}

public sealed record ObjectRead(StoredObjectRef Object, Stream Content);
// Input stream remains caller-owned; caller disposes successful OpenRead Content.
// Implementations enforce workspace/root/symlink/hash/byte limits and current local access.
public interface IObjectStorage
{
    Task<Result<StoredObjectRef>> PutAsync(ObjectWriteRequest request, Stream content, CancellationToken ct);
    Task<Result<StoredObjectRef>> GetInfoAsync(WorkspaceId workspace, string key, CancellationToken ct);
    Task<Result<ObjectRead>> OpenReadAsync(WorkspaceId workspace, string key, long maxBytes, CancellationToken ct);
}
