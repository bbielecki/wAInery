namespace wAInery.BuildingBlocks;

public sealed record CommandContext
{
    public WorkspaceId Workspace { get; }
    public string IdempotencyKey { get; }
    public Guid CorrelationId { get; }
    public string ActorId { get; }
    public long? ExpectedVersion { get; }
    public Guid? CausationId { get; }

    public CommandContext(WorkspaceId workspace, string idempotencyKey, Guid correlationId,
        string actorId, long? expectedVersion = null, Guid? causationId = null)
    {
        if (workspace.Value == Guid.Empty || correlationId == Guid.Empty || causationId == Guid.Empty)
            throw new ArgumentException("Command workspace and correlation identities must be valid.");
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (expectedVersion is < 0) throw new ArgumentOutOfRangeException(nameof(expectedVersion));
        Workspace = workspace;
        IdempotencyKey = idempotencyKey;
        CorrelationId = correlationId;
        ActorId = actorId;
        ExpectedVersion = expectedVersion;
        CausationId = causationId;
    }
}
