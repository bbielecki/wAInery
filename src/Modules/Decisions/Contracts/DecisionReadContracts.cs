using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Decisions.Contracts;

public enum Feasibility { FEASIBLE, CONDITIONAL, NOT_FEASIBLE, UNKNOWN }
public enum PlanState { CURRENT, STALE, INSUFFICIENT_DATA, FAILED, SUPERSEDED }
public enum PredictionStatus { CURRENT, STALE, RECOMPUTING, FAILED, INSUFFICIENT_DATA }
public enum CalibrationStatus { VALIDATED, UNVALIDATED, EXPERT_ESTIMATE }
public sealed record ProbabilityDto
{
    public decimal Value { get; }
    public string Method { get; }
    public string ModelVersion { get; }
    public CalibrationStatus CalibrationStatus { get; }
    public string DataBasis { get; }
    public string? EvaluationRef { get; }
    public ProbabilityDto(decimal value, string method, string modelVersion, CalibrationStatus calibrationStatus,
        string dataBasis, string? evaluationRef = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(method);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataBasis);
        if (value is < 0 or > 1 || !Enum.IsDefined(calibrationStatus)) throw new ArgumentException("Invalid probability metadata.");
        (Value, Method, ModelVersion, CalibrationStatus, DataBasis, EvaluationRef) =
            (value, method, modelVersion, calibrationStatus, dataBasis, evaluationRef);
    }
}
public sealed record PlanPredictionDto(string PredictionId, string PredictionKind, PredictionStatus Status,
    DateTimeOffset HorizonStart, DateTimeOffset HorizonEnd, ImmutableArray<string> EvidenceRefs, ProbabilityDto? EventProbability = null);
public sealed record PlanActionDto(string ActionId, ActionType ActionType, string Description,
    ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence, Feasibility Feasibility, bool RequiresUserApproval,
    decimal? RankingScore = null, DateTimeOffset? Deadline = null, ImmutableArray<string> PrerequisiteActionIds = default,
    string? ExclusivityGroup = null, ProbabilityDto? EffectivenessProbability = null)
{
    private ImmutableArray<string> prerequisites = PrerequisiteActionIds.IsDefault ? [] : PrerequisiteActionIds;
    public ImmutableArray<string> PrerequisiteActionIds
    {
        get => prerequisites;
        init => prerequisites = value.IsDefault ? [] : value;
    }
}
public sealed record DecisionPlanDto(Guid PlanId, WorkspaceId Workspace, SubjectRef Subject, long PlanRevision,
    PlanState State, DateTimeOffset AsOf, long InputRevision, ImmutableArray<string> EvidenceRefs,
    ImmutableArray<PlanPredictionDto> Predictions, ImmutableArray<PlanActionDto> Actions,
    ImmutableArray<string> SelectedActionIds, ImmutableArray<string> AlternativeActionIds, string Reason,
    string? InputSnapshotRef = null, DateTimeOffset? CreatedAt = null)
{
    public int SchemaVersion => 1;
}
public sealed record DecisionScope
{
    public WorkspaceId Workspace { get; }
    public SubjectRef Subject { get; }
    public string PredictionCode { get; }
    public string DefinitionVersion { get; }
    public TimeWindow Horizon { get; }
    public DecisionScope(WorkspaceId workspace, SubjectRef subject, string predictionCode, string definitionVersion, TimeWindow horizon)
    {
        DecisionContractGuard.Scope(workspace, subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(predictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(definitionVersion);
        if (horizon.To <= horizon.From) throw new ArgumentException("A valid decision horizon is required.");
        (Workspace, Subject, PredictionCode, DefinitionVersion, Horizon) =
            (workspace, subject, predictionCode, definitionVersion, new(horizon.From.ToUniversalTime(), horizon.To.ToUniversalTime()));
    }
}
public sealed record ImpactAnalysisDto(ImmutableArray<DecisionScope> Scopes, bool RequiresCoverageAudit);
public interface IImpactAnalyzer
{
    Task<ImpactAnalysisDto> AffectedAsync(DomainChangeV1 change, CancellationToken ct);
}

// IPC wrapper only. These states never extend the existing PlanV1.state enum.
public enum ReadFreshness { CURRENT, STALE, PENDING_REEVALUATION }
public sealed record DecisionFreshnessDto
{
    public ReadFreshness Status { get; }
    public DateTimeOffset CheckedAt { get; }
    public bool ImpactCaughtUp { get; }
    public bool HasPendingAcceptedChanges { get; }
    public DateTimeOffset? ImpactCaughtUpThrough { get; }
    public long InputRevision { get; }
    public long? PublishedRevision { get; }
    public bool DefinitionVersionMatches { get; }
    public DateTimeOffset? ValidUntil { get; }
    public DecisionFreshnessDto(ReadFreshness status, DateTimeOffset checkedAt, bool impactCaughtUp,
        bool hasPendingAcceptedChanges, DateTimeOffset? impactCaughtUpThrough, long inputRevision,
        long? publishedRevision, bool definitionVersionMatches, DateTimeOffset? validUntil)
    {
        if (!Enum.IsDefined(status) || inputRevision < 1 || publishedRevision is < 1 || publishedRevision > inputRevision)
            throw new ArgumentException("Invalid freshness revision metadata.");
        if (status == ReadFreshness.CURRENT && (!impactCaughtUp || hasPendingAcceptedChanges || !definitionVersionMatches
            || publishedRevision != inputRevision || validUntil is null || validUntil <= checkedAt
            || impactCaughtUpThrough is null || impactCaughtUpThrough > checkedAt))
            throw new ArgumentException("Current freshness requires explicit catch-up, revision, definition and expiry proof.");
        (Status, CheckedAt, ImpactCaughtUp, HasPendingAcceptedChanges, ImpactCaughtUpThrough, InputRevision,
            PublishedRevision, DefinitionVersionMatches, ValidUntil) =
            (status, checkedAt.ToUniversalTime(), impactCaughtUp, hasPendingAcceptedChanges, impactCaughtUpThrough?.ToUniversalTime(),
            inputRevision, publishedRevision, definitionVersionMatches, validUntil?.ToUniversalTime());
    }
}
public sealed record DecisionReadDto(DecisionPlanDto Plan, DecisionFreshnessDto Freshness, ImmutableArray<DecisionScope> Scopes);
public sealed record PredictionReadDto(DecisionScope Scope, PlanPredictionDto Prediction, DateTimeOffset AsOf,
    DateTimeOffset KnownAt, DecisionFreshnessDto Freshness);
public interface IDecisionReadPort
{
    Task<Result<DecisionReadDto>> GetLatestAsync(SubjectRef subject, CancellationToken ct);
    Task<Result<DecisionReadDto>> GetPlanAsync(WorkspaceId workspace, Guid planId, long? planRevision, CancellationToken ct);
    Task<Result<PredictionReadDto>> GetPredictionAsync(DecisionScope scope, DateTimeOffset asOf, CancellationToken ct);
}
public enum DecisionFeedbackKind { Accepted, Rejected, PerformedElsewhere, OutcomeObserved }
public sealed record RecordDecisionFeedbackCommand(CommandContext Context, SubjectRef Subject, Guid PlanId,
    long PlanRevision, string ActionId, DecisionFeedbackKind Kind, string? Comment = null) : SubjectCommand(Context, Subject);
public sealed record RecomputePredictionCommand
{
    public CommandContext Context { get; }
    public DecisionScope Scope { get; }
    public DateTimeOffset AsOf { get; }
    public string ReasonCode { get; }
    public RecomputePredictionCommand(CommandContext context, DecisionScope scope, DateTimeOffset asOf, string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (context.Workspace != scope.Workspace) throw new ArgumentException("Recompute command scope mismatch.");
        (Context, Scope, AsOf, ReasonCode) = (context, scope, asOf.ToUniversalTime(), reasonCode);
    }
}
public sealed record BuildDecisionPlanCommand : SubjectCommand
{
    public ImmutableArray<DecisionScope> Scopes { get; }
    public DateTimeOffset AsOf { get; }
    public BuildDecisionPlanCommand(CommandContext context, SubjectRef subject, IEnumerable<DecisionScope> scopes,
        DateTimeOffset asOf) : base(context, subject)
    {
        ArgumentNullException.ThrowIfNull(scopes);
        var copy = scopes.ToImmutableArray();
        if (copy.Any(scope => scope is null || scope.Workspace != context.Workspace || scope.Subject != subject))
            throw new ArgumentException("Plan scopes must match the command target.");
        (Scopes, AsOf) = (copy, asOf.ToUniversalTime());
    }
}
public interface IDecisionCommandPort
{
    Task<Result<CommandReceipt>> RecomputeAsync(RecomputePredictionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> BuildPlanAsync(BuildDecisionPlanCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RecordFeedbackAsync(RecordDecisionFeedbackCommand command, CancellationToken ct);
}
