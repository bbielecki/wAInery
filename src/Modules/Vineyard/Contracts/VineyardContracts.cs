using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Vineyard.Contracts;

public sealed record VarietyPlantingDto(string VarietyCode, decimal? Fraction, int? PlantedYear);
public sealed record PhenologyDto(string StageCode, string Method, DateTimeOffset ObservedAt, DateTimeOffset RecordedAt, long Version);
public sealed record VineyardActivityDto(string Id, string OperationType, DateTimeOffset PerformedAt,
    DateTimeOffset RecordedAt, string ConfirmedBy, string? CorrectsId, string? Description = null,
    Quantity? Amount = null, string? MaterialCode = null);
public sealed record BlockSnapshot(SubjectRef Subject, long Version, DateTimeOffset AsOf, DateTimeOffset KnownAt,
    string Name, decimal? AreaM2, ImmutableArray<VarietyPlantingDto> Plantings, PhenologyDto? Phenology,
    ImmutableArray<VineyardActivityDto> ConfirmedActivities, string? WeatherLocationId = null);
public sealed record BlockReferencePage(WorkspaceId Workspace, string WeatherLocationId, DateTimeOffset AsOf,
    DateTimeOffset KnownAt, ImmutableArray<SubjectRef> Subjects, string? NextCursor);
public sealed record VineyardProposalDto(string ProposalId, SubjectRef Subject, string OperationType,
    string Description, DateTimeOffset? SuggestedPerformedAt, long Version, Quantity? Amount = null, string? MaterialCode = null);

public sealed record RegisterBlockCommand(CommandContext Context, SubjectRef Subject, string Name,
    decimal? AreaM2 = null, string? WeatherLocationId = null)
    : SubjectCommand(Context, Subject, "Vineyard", "Block");
public sealed record UpdateBlockLocationCommand(CommandContext Context, SubjectRef Subject, string? WeatherLocationId)
    : SubjectCommand(Context, Subject, "Vineyard", "Block");
public sealed record UpdatePlantingsCommand(CommandContext Context, SubjectRef Subject, ImmutableArray<VarietyPlantingDto> Plantings)
    : SubjectCommand(Context, Subject, "Vineyard", "Block");
public sealed record ConfirmPhenologyCommand(CommandContext Context, SubjectRef Subject, string StageCode,
    string Method, DateTimeOffset ObservedAt) : SubjectCommand(Context, Subject, "Vineyard", "Block");
public sealed record ProposeVineyardActivityCommand(CommandContext Context, SubjectRef Subject,
    string OperationType, string Description, DateTimeOffset? SuggestedPerformedAt = null,
    Quantity? Amount = null, string? MaterialCode = null)
    : SubjectCommand(Context, Subject, "Vineyard", "Block");
// Explicit owner review step; creates a challenge, never a performed operation.
public sealed record PrepareVineyardConfirmationCommand(CommandContext Context, SubjectRef Subject,
    string ProposalId, DateTimeOffset PerformedAt) : SubjectCommand(Context, Subject, "Vineyard", "Block");
public sealed record ConfirmVineyardActivityCommand(CommandContext Context, SubjectRef Subject, OperationConfirmation Confirmation)
    : SubjectCommand(Context, Subject, "Vineyard", "Block")
{
    public OperationConfirmation Confirmation { get; } = Confirmation ?? throw new ArgumentNullException(nameof(Confirmation));
}
public sealed record CorrectVineyardActivityCommand(CommandContext Context, SubjectRef Subject,
    string CorrectsId, OperationConfirmation Confirmation) : SubjectCommand(Context, Subject, "Vineyard", "Block")
{
    public OperationConfirmation Confirmation { get; } = Confirmation ?? throw new ArgumentNullException(nameof(Confirmation));
}

public interface IVineyardReadPort
{
    Task<Result<ResolvedSubject>> ResolveAsync(SubjectRef block, CancellationToken ct);
    Task<Result<BlockSnapshot>> GetBlockAsync(SubjectRef block, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<BlockReferencePage>> FindByWeatherLocationAsync(WorkspaceId workspace, string weatherLocationId,
        DateTimeOffset asOf, int maxCount, string? cursor, CancellationToken ct);
}

public interface IVineyardCommandPort
{
    Task<Result<CommandReceipt>> RegisterBlockAsync(RegisterBlockCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> UpdateLocationAsync(UpdateBlockLocationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> UpdatePlantingsAsync(UpdatePlantingsCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> ConfirmPhenologyAsync(ConfirmPhenologyCommand command, CancellationToken ct);
    Task<Result<VineyardProposalDto>> ProposeActivityAsync(ProposeVineyardActivityCommand command, CancellationToken ct);
    Task<Result<OperationConfirmation>> PrepareConfirmationAsync(PrepareVineyardConfirmationCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> ConfirmActivityAsync(ConfirmVineyardActivityCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> CorrectActivityAsync(CorrectVineyardActivityCommand command, CancellationToken ct);
}
