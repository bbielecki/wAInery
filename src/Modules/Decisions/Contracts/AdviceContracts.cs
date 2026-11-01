using System.Collections.Immutable;
using System.Text.Json;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Decisions.Contracts;

public enum AdvicePurpose { PRIVATE_ADVICE, EVALUATION }
public enum AdvisorRole { VINEYARD_ADVISOR, WINEMAKING_ADVISOR }
public enum AdviceTool { SEARCH_EVIDENCE, GET_OBSERVATIONS, GET_WEATHER, GET_DOMAIN_CONTEXT }
public enum AdviceStatus { OK, INSUFFICIENT_DATA, UNAVAILABLE, POLICY_BLOCKED, ERROR }
public enum EvidenceConfidence { LOW, MEDIUM, HIGH, UNKNOWN }
public enum ActionType { MEASURE, OBSERVE, NO_ACTION, ESCALATE, TREATMENT, PROCESS_ADJUSTMENT, OTHER }

internal static class DecisionContractGuard
{
    internal static bool Classification(string? value) => value is null or "PRIVATE_COPYRIGHT" or "PRIVATE_DERIVED"
        or "PRIVATE_OPERATIONAL" or "PUBLIC_LICENSED" or "PUBLIC_REFERENCE";
    internal static void Scope(WorkspaceId workspace, SubjectRef subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        if (workspace.Value == Guid.Empty || !subject.HasValidScope || subject.Workspace != workspace ||
            subject.Module is not ("Vineyard" or "Winery" or "Observations" or "Weather" or "Knowledge" or "Connectivity" or "Decisions"))
            throw new ArgumentException("Decision subject scope mismatch.");
    }
    internal static ImmutableArray<T> Snapshot<T>(IEnumerable<T> values, int limit)
    {
        ArgumentNullException.ThrowIfNull(values);
        var copy = values.Take(limit + 1).ToImmutableArray();
        if (copy.Length > limit || copy.Any(item => item is null)) throw new ArgumentException("Invalid bounded decision context.");
        return copy;
    }
}

public sealed record AdviceFact
{
    public string Key { get; }
    public JsonElement Value { get; }
    public string SourceRef { get; }
    public string? Unit { get; }
    public string? Classification { get; }
    public AdviceFact(string key, JsonElement value, string sourceRef, string? unit = null, string? classification = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceRef);
        if (value.ValueKind is not (JsonValueKind.Number or JsonValueKind.String or JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null)
            || !DecisionContractGuard.Classification(classification)) throw new ArgumentException("Invalid scalar fact metadata.");
        (Key, Value, SourceRef, Unit, Classification) = (key, value.Clone(), sourceRef, unit, classification);
    }
}

public sealed record AgentAdviceRequest
{
    public int SchemaVersion => 1;
    public string OutputSchemaId => "AgentAdviceResultV1";
    public Guid RequestId { get; }
    public WorkspaceId Workspace { get; }
    public AdvicePurpose Purpose { get; }
    public AdvisorRole Role { get; }
    public SubjectRef Subject { get; }
    public DateTimeOffset AsOf { get; }
    public long InputRevision { get; }
    public ImmutableArray<SourceVersionRef> SourceVersions { get; }
    public ImmutableArray<string> EvidenceRefs { get; }
    public ImmutableArray<AdviceFact> Facts { get; }
    public ImmutableArray<AdviceTool> AllowedTools { get; }
    public int MaxInputTokens { get; }
    public int MaxOutputTokens { get; }
    public decimal MaxCostUsd { get; }
    public int TimeoutMs { get; }
    public AgentAdviceRequest(Guid requestId, WorkspaceId workspace, AdvicePurpose purpose, AdvisorRole role,
        SubjectRef subject, DateTimeOffset asOf, long inputRevision, IEnumerable<SourceVersionRef> sourceVersions,
        IEnumerable<string> evidenceRefs, IEnumerable<AdviceFact> facts, IEnumerable<AdviceTool> allowedTools,
        int maxInputTokens, int maxOutputTokens, decimal maxCostUsd, int timeoutMs)
    {
        DecisionContractGuard.Scope(workspace, subject);
        if (requestId == Guid.Empty || inputRevision < 1 || !Enum.IsDefined(purpose) || !Enum.IsDefined(role)
            || maxInputTokens < 1 || maxOutputTokens < 1 || maxCostUsd < 0 || timeoutMs < 1000)
            throw new ArgumentException("Invalid advice identity, role or v1 budget bounds.");
        var sources = DecisionContractGuard.Snapshot(sourceVersions, 512);
        var evidence = DecisionContractGuard.Snapshot(evidenceRefs, 256);
        var factCopy = DecisionContractGuard.Snapshot(facts, 512);
        var tools = DecisionContractGuard.Snapshot(allowedTools, 4);
        if (sources.Any(source => string.IsNullOrWhiteSpace(source.SourceId) || string.IsNullOrWhiteSpace(source.Version)
            || !DecisionContractGuard.Classification(source.Classification)) || evidence.Any(string.IsNullOrWhiteSpace)
            || tools.Any(tool => !Enum.IsDefined(tool)) || tools.Distinct().Count() != tools.Length)
            throw new ArgumentException("Invalid advice provenance or read-only tool allowance.");
        (RequestId, Workspace, Purpose, Role, Subject, AsOf, InputRevision, SourceVersions, EvidenceRefs, Facts, AllowedTools,
            MaxInputTokens, MaxOutputTokens, MaxCostUsd, TimeoutMs) =
            (requestId, workspace, purpose, role, subject, asOf.ToUniversalTime(), inputRevision, sources, evidence, factCopy,
            tools, maxInputTokens, maxOutputTokens, maxCostUsd, timeoutMs);
    }
}
public sealed record AdviceFinding(string FindingId, string Statement, ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence);
public sealed record AdviceActionCandidate(string CandidateId, ActionType ActionType, string Description,
    ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence, bool RequiresUserApproval,
    DateTimeOffset? NotBefore = null, DateTimeOffset? Deadline = null,
    ImmutableArray<string> PrerequisiteCandidateIds = default, string? ExclusivityGroup = null)
{
    private ImmutableArray<string> prerequisites = PrerequisiteCandidateIds.IsDefault ? [] : PrerequisiteCandidateIds;
    public ImmutableArray<string> PrerequisiteCandidateIds
    {
        get => prerequisites;
        init => prerequisites = value.IsDefault ? [] : value;
    }
}
public sealed record AdviceUsage(string Provider, string Model, int InputTokens, int OutputTokens, decimal EstimatedCostUsd);
// Untrusted proposal only. The owner validates request/evidence/schema/business safety before publishing.
public sealed record AgentAdviceResult(Guid RequestId, AdviceStatus Status, ImmutableArray<AdviceFinding> Findings,
    ImmutableArray<AdviceActionCandidate> ActionCandidates, AdviceUsage Usage, string? ErrorCode = null)
{
    public int SchemaVersion => 1;
}
public interface IReasoningExecutor
{
    Task<AgentAdviceResult> AdviseAsync(AgentAdviceRequest request, CancellationToken ct);
}
