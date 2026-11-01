using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Winery.Contracts;

public sealed record VesselDto(WorkspaceId Workspace, string VesselId, string Name, string VesselType, Quantity? Capacity);
public sealed record VesselOccupancyDto(string BatchId, string VesselId, DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, Quantity? Volume);
public sealed record CellarOperationDto(string Id, string OperationType, DateTimeOffset PerformedAt,
    DateTimeOffset RecordedAt, string ConfirmedBy, string? CorrectsId, string? Description = null,
    Quantity? Amount = null, string? MaterialCode = null);
public sealed record BatchSnapshot(SubjectRef Subject, long Version, DateTimeOffset AsOf, DateTimeOffset KnownAt,
    string StateCode, string? ProcessStage, ImmutableArray<VesselOccupancyDto> Occupancies,
    ImmutableArray<CellarOperationDto> ConfirmedOperations);
public sealed record CellarProposalDto(string ProposalId, SubjectRef Subject, string OperationType,
    string Description, DateTimeOffset? SuggestedPerformedAt, long Version, Quantity? Amount = null, string? MaterialCode = null);

public sealed record CreateWineBatchCommand(CommandContext Context, SubjectRef Subject, string Code, string StateCode)
    : SubjectCommand(Context, Subject, "Winery", "WineBatch");
public sealed record RegisterVesselCommand(CommandContext Context, SubjectRef Subject, string Name, string VesselType, Quantity? Capacity)
    : SubjectCommand(Context, Subject, "Winery", "Vessel");
public sealed record AssignBatchToVesselCommand(CommandContext Context, SubjectRef Subject, string VesselId,
    DateTimeOffset ValidFrom, DateTimeOffset? ValidTo, Quantity? Volume) : SubjectCommand(Context, Subject, "Winery", "WineBatch");
public sealed record ProposeCellarOperationCommand(CommandContext Context, SubjectRef Subject,
    string OperationType, string Description, DateTimeOffset? SuggestedPerformedAt = null,
    Quantity? Amount = null, string? MaterialCode = null)
    : SubjectCommand(Context, Subject, "Winery", "WineBatch");
public sealed record PrepareCellarConfirmationCommand(CommandContext Context, SubjectRef Subject,
    string ProposalId, DateTimeOffset PerformedAt) : SubjectCommand(Context, Subject, "Winery", "WineBatch");
public sealed record ConfirmCellarOperationCommand(CommandContext Context, SubjectRef Subject, OperationConfirmation Confirmation)
    : SubjectCommand(Context, Subject, "Winery", "WineBatch")
{
    public OperationConfirmation Confirmation { get; } = Confirmation ?? throw new ArgumentNullException(nameof(Confirmation));
}
public sealed record CorrectCellarOperationCommand(CommandContext Context, SubjectRef Subject, string CorrectsId,
    OperationConfirmation Confirmation) : SubjectCommand(Context, Subject, "Winery", "WineBatch")
{
    public OperationConfirmation Confirmation { get; } = Confirmation ?? throw new ArgumentNullException(nameof(Confirmation));
}

public interface IWineryReadPort
{
    Task<Result<ResolvedSubject>> ResolveAsync(SubjectRef subject, CancellationToken ct);
    Task<Result<BatchSnapshot>> GetBatchAsync(SubjectRef batch, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<VesselDto>> GetVesselAsync(WorkspaceId workspace, string vesselId, CancellationToken ct);
}

public interface IWineryCommandPort
{
    Task<Result<CommandReceipt>> CreateBatchAsync(CreateWineBatchCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RegisterVesselAsync(RegisterVesselCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> AssignBatchToVesselAsync(AssignBatchToVesselCommand command, CancellationToken ct);
    Task<Result<CellarProposalDto>> ProposeOperationAsync(ProposeCellarOperationCommand command, CancellationToken ct);
    Task<Result<OperationConfirmation>> PrepareConfirmationAsync(PrepareCellarConfirmationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> ConfirmOperationAsync(ConfirmCellarOperationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> CorrectOperationAsync(CorrectCellarOperationCommand command, CancellationToken ct);
}
