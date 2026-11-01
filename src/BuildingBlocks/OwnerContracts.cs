namespace wAInery.BuildingBlocks;

// A resolution result is produced by the owning module, never decoded from caller input.
public sealed record ResolvedSubject
{
    public SubjectRef Subject { get; }
    public Guid InternalId { get; }
    public long Version { get; }
    public ResolvedSubject(SubjectRef subject, Guid internalId, long version)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (!subject.HasValidScope || internalId == Guid.Empty) throw new ArgumentException("A scoped canonical identity is required.");
        if (version <= 0) throw new ArgumentOutOfRangeException(nameof(version));
        Subject = subject;
        InternalId = internalId;
        Version = version;
    }
}

public abstract record SubjectCommand
{
    public CommandContext Context { get; }
    public SubjectRef Subject { get; }
    protected SubjectCommand(CommandContext context, SubjectRef subject, string? module = null, string? kind = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(subject);
        if (!subject.HasValidScope || context.Workspace != subject.Workspace ||
            (module is not null && subject.Module != module) || (kind is not null && subject.Kind != kind))
            throw new ArgumentException("Command subject scope/owner mismatch.");
        Context = context;
        Subject = subject;
    }
}

public sealed record CommandReceipt(string Id, long Version, DateTimeOffset RecordedAt, bool Duplicate);

// Token binds an owner-created proposal to its explicitly verified target/time.
// Authorization/token verification belongs to the owner implementation.
public sealed record OperationConfirmation
{
    public string ProposalId { get; }
    public string Token { get; }
    public DateTimeOffset PerformedAt { get; }
    public OperationConfirmation(string proposalId, string token, DateTimeOffset performedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proposalId);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ProposalId = proposalId;
        Token = token;
        PerformedAt = performedAt.ToUniversalTime();
    }
}
