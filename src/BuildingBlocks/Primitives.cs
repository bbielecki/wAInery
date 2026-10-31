namespace wAInery.BuildingBlocks;

// Public identities are opaque; internal persistence UUIDs are resolved by the owning module.
public readonly record struct WorkspaceId(Guid Value);

public sealed record SubjectRef(WorkspaceId Workspace, string Module, string Kind, string Id)
{
    public bool HasValidScope =>
        Workspace.Value != Guid.Empty &&
        !string.IsNullOrWhiteSpace(Module) &&
        !string.IsNullOrWhiteSpace(Kind) &&
        !string.IsNullOrWhiteSpace(Id);
}

public sealed record EvidenceRef(string SourceId, string Version, string Locator);

public readonly record struct TimeWindow
{
    public DateTimeOffset From { get; }
    public DateTimeOffset To { get; }

    public TimeWindow(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from) throw new ArgumentOutOfRangeException(nameof(to), "End must be after start.");
        From = from;
        To = to;
    }
}

public sealed record Quantity(decimal Value, string Unit, string? Method = null);
public sealed record DomainError(string Code, string Message, bool Retryable, string CorrelationId);
public sealed record Result<T>(T? Value, DomainError? Error);
