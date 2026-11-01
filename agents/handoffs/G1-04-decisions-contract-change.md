# G1-04 Decisions contract-change proposal

Issue [#30](https://github.com/bbielecki/wAInery/issues/30), Gate 1 #10; BR-01/03/04/09/10.
Owner Decisions / phase analyst `/root/stream_b_connectivity`; freeze owner IO.
Status Proposed; base `69780c7`; application API cost 0. This file only changes.
No source/tests/migration/pricing changes or inference/ranking implementation.

References: [Decisions](../../docs/30-components/Decisions.md),
[ports](../../docs/90-lld/ports-and-compatibility.md),
[freeze](../../docs/90-lld/implementation-contract-freeze.md),
[incremental freshness](../../docs/90-lld/incremental-prediction.md).
Exact JSON schemas/examples remain canonical; these are new IPC constructors,
not a competing schema. IO owns the public contract PR and independent tests.

## Ownership, reuse and wire mapping

`Decisions.Contracts` references BB only; BCL ImmutableArray/System.Text.Json need
no extra package/framework. Reuse BB SubjectRef, WorkspaceId, TimeWindow,
SourceVersionRef, CommandContext, SubjectCommand, CommandReceipt and Result.
AIExecution's later implementation references Decisions.Contracts; never reverse.
Reuse existing JsonSchema.Net in contract tests, not a schema engine in Contracts.

Wire workspaceId stays at the root; IPC Workspace/Subject.Workspace must match.
Subject/source/prediction/action/evidence IDs remain opaque; only requestId and
planId are UUIDs where schemas require. Explicit wire mapping validates version1,
RFC3339/UTC, strict unknown fields and optional absence/null rules. Direct default
JsonSerializer serialization is not claimed. No intent/locale/currency/decisionScope
fields are added to the request. Required facts.value may be null and must remain
present. Optional nullable CLR fields map to omission except schema-permitted null
probabilities/rankingScore. Plan actions have deadline, not a new notBefore field.

## Proposed IPC signatures

Enums below intentionally spell exact existing wire literals. Records show
constructor names/fields, not guard implementations. IO must freeze immutable
validated values, copy inputs and reject undefined enum/JsonElement values.
`AdviceFact.Value` clones JsonElement and permits only number/string/bool/null;
objects/arrays/Undefined are rejected. No bespoke scalar framework is needed.

```csharp
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
public enum Feasibility { FEASIBLE, CONDITIONAL, NOT_FEASIBLE, UNKNOWN }
public enum PlanState { CURRENT, STALE, INSUFFICIENT_DATA, FAILED, SUPERSEDED }
public enum PredictionStatus { CURRENT, STALE, RECOMPUTING, FAILED, INSUFFICIENT_DATA }
public enum CalibrationStatus { VALIDATED, UNVALIDATED, EXPERT_ESTIMATE }
public sealed record AdviceFact(string Key, JsonElement Value, string SourceRef,
    string? Unit = null, string? Classification = null);
public sealed record AgentAdviceRequest(Guid RequestId, WorkspaceId Workspace,
    AdvicePurpose Purpose, AdvisorRole Role, SubjectRef Subject, DateTimeOffset AsOf,
    long InputRevision, ImmutableArray<SourceVersionRef> SourceVersions,
    ImmutableArray<string> EvidenceRefs, ImmutableArray<AdviceFact> Facts,
    ImmutableArray<AdviceTool> AllowedTools, int MaxInputTokens, int MaxOutputTokens,
    decimal MaxCostUsd, int TimeoutMs)
{ public int SchemaVersion => 1; public string OutputSchemaId => "AgentAdviceResultV1"; }
public sealed record AdviceFinding(string FindingId, string Statement,
    ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence);
public sealed record AdviceActionCandidate(string CandidateId, ActionType ActionType,
    string Description, ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence,
    bool RequiresUserApproval, DateTimeOffset? NotBefore = null, DateTimeOffset? Deadline = null,
    ImmutableArray<string> PrerequisiteCandidateIds = default, string? ExclusivityGroup = null);
public sealed record AdviceUsage(string Provider, string Model, int InputTokens,
    int OutputTokens, decimal EstimatedCostUsd);
public sealed record AgentAdviceResult(Guid RequestId, AdviceStatus Status,
    ImmutableArray<AdviceFinding> Findings, ImmutableArray<AdviceActionCandidate> ActionCandidates,
    AdviceUsage Usage, string? ErrorCode = null)
{ public int SchemaVersion => 1; }
public interface IReasoningExecutor
{ Task<AgentAdviceResult> AdviseAsync(AgentAdviceRequest request, CancellationToken ct); }

public sealed record ProbabilityDto(decimal Value, string Method, string ModelVersion,
    CalibrationStatus CalibrationStatus, string DataBasis, string? EvaluationRef = null);
public sealed record PlanPredictionDto(string PredictionId, string PredictionKind,
    PredictionStatus Status, DateTimeOffset HorizonStart, DateTimeOffset HorizonEnd,
    ImmutableArray<string> EvidenceRefs, ProbabilityDto? EventProbability = null);
public sealed record PlanActionDto(string ActionId, ActionType ActionType, string Description,
    ImmutableArray<string> EvidenceRefs, EvidenceConfidence Confidence, Feasibility Feasibility,
    bool RequiresUserApproval, decimal? RankingScore = null, DateTimeOffset? Deadline = null,
    ImmutableArray<string> PrerequisiteActionIds = default, string? ExclusivityGroup = null,
    ProbabilityDto? EffectivenessProbability = null);
public sealed record DecisionPlanDto(Guid PlanId, WorkspaceId Workspace, SubjectRef Subject,
    long PlanRevision, PlanState State, DateTimeOffset AsOf, long InputRevision,
    ImmutableArray<string> EvidenceRefs, ImmutableArray<PlanPredictionDto> Predictions,
    ImmutableArray<PlanActionDto> Actions, ImmutableArray<string> SelectedActionIds,
    ImmutableArray<string> AlternativeActionIds, string Reason,
    string? InputSnapshotRef = null, DateTimeOffset? CreatedAt = null)
{ public int SchemaVersion => 1; }

// IPC only: explicit DefinitionVersion and coverage flag approved by IO in #30.
public sealed record DecisionScope(WorkspaceId Workspace, SubjectRef Subject,
    string PredictionCode, string DefinitionVersion, TimeWindow Horizon);
public sealed record ImpactAnalysisDto(ImmutableArray<DecisionScope> Scopes, bool RequiresCoverageAudit);
public interface IImpactAnalyzer
{ Task<ImpactAnalysisDto> AffectedAsync(DomainChangeV1 change, CancellationToken ct); }

// These enums/metadata are IPC/read-wrapper only, never PlanV1.state members.
public enum ReadFreshness { CURRENT, STALE, PENDING_REEVALUATION }
public sealed record DecisionFreshnessDto(ReadFreshness Status, DateTimeOffset CheckedAt,
    bool ImpactCaughtUp, bool HasPendingAcceptedChanges, DateTimeOffset? ImpactCaughtUpThrough,
    long InputRevision, long? PublishedRevision, bool DefinitionVersionMatches, DateTimeOffset? ValidUntil);
public sealed record DecisionReadDto(DecisionPlanDto Plan, DecisionFreshnessDto Freshness,
    ImmutableArray<DecisionScope> Scopes);
public sealed record PredictionReadDto(DecisionScope Scope, PlanPredictionDto Prediction,
    DateTimeOffset AsOf, DateTimeOffset KnownAt, DecisionFreshnessDto Freshness);
public interface IDecisionReadPort
{
    Task<Result<DecisionReadDto>> GetLatestAsync(SubjectRef subject, CancellationToken ct);
    Task<Result<DecisionReadDto>> GetPlanAsync(WorkspaceId workspace, Guid planId, long? planRevision, CancellationToken ct);
    Task<Result<PredictionReadDto>> GetPredictionAsync(DecisionScope scope, DateTimeOffset asOf, CancellationToken ct);
}
public enum DecisionFeedbackKind { Accepted, Rejected, PerformedElsewhere, OutcomeObserved }
public sealed record RecordDecisionFeedbackCommand(CommandContext Context, SubjectRef Subject,
    Guid PlanId, long PlanRevision, string ActionId, DecisionFeedbackKind Kind, string? Comment = null)
    : SubjectCommand(Context, Subject);
public sealed record RecomputePredictionCommand(CommandContext Context, DecisionScope Scope,
    DateTimeOffset AsOf, string ReasonCode);
public sealed record BuildDecisionPlanCommand(CommandContext Context, SubjectRef Subject,
    ImmutableArray<DecisionScope> Scopes, DateTimeOffset AsOf) : SubjectCommand(Context, Subject);
public interface IDecisionCommandPort
{
    Task<Result<CommandReceipt>> RecomputeAsync(RecomputePredictionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> BuildPlanAsync(BuildDecisionPlanCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RecordFeedbackAsync(RecordDecisionFeedbackCommand command, CancellationToken ct);
}
```

## Validation and first-slice semantics

Request/plan inputRevision and planRevision >=1; required token caps >=1,
maxCostUsd >=0 (zero is a real zero ceiling), timeoutMs >=1000, usage tokens/cost
>=0. No new price/default/monthly cap. Further execution/collection limits belong
to existing configured policy/token budgets, without silently rewriting schemas.
Only listed read tools; sourceVersions.classification/facts.classification use
the five exact existing classes or omitted/unknown-denied internal state, never
a new serialized enum. #27 shared current rights/proof types remain IO-owned;
classification alone is not an egress grant, EVALUATION is not a policy bypass.
Provider failure produces the existing status and safe error code with zero/factual
usage; do not invent billed amounts, prompt fields or additional usage wire fields.

All model results are untrusted proposals; requestId must match and evidence refs
must resolve/fit permitted context. Schema acceptance is not business validation.
No numeric probability field exists in AdviceResult. Plan ProbabilityDto permits
0..1 only with all five required metadata fields; EvaluationRef remains optional.
UNVALIDATED and EXPERT_ESTIMATE remain legal v1 labels, not claims of calibration.
Metadata presence never certifies quality; future owner/presentation enforces
accepted scientific/evidence constraints. RankingScore is not a probability.
Approval flags never authorize performed work; feedback is not a domain operation.

Scope validates workspace equality, nonblank code/version, valid half-open horizon
and owner subject. DefinitionVersion prevents historical version aliasing. Impact
returns bounded scopes; unknown Knowledge tags request a bounded coverage audit,
not empty no-impact or global invalidation. No impact algorithm is implemented.
Read errors use existing safe NOT_FOUND/VALIDATION/UNAVAILABLE etc. Complete arrays
can be empty where schemas allow; optional prerequisite defaults normalize to Empty.

Freshness is separate from immutable stored plan state. Pending accepted changes
or unproved catch-up expose PENDING_REEVALUATION without invalidating unrelated
predictions. CURRENT freshness requires caught-up processing, no pending changes,
matching definition/input/published revisions and unexpired known ValidUntil.
Unknown expiry/revision/catch-up cannot certify CURRENT. Pending coverage audits
also prevent caught-up status; outbox acknowledgement alone is insufficient. Timestamps are audit
watermarks, not proof that no unhandled committed event exists. A handler must
check that condition consistently with its read snapshot. Freshness never upgrades
Plan.State (e.g. INSUFFICIENT_DATA) or changes a historical plan. Actual catch-up,
as-of reads, stale-run CAS and atomic publication remain later implementation.
Plan reads carry versioned scopes; GetPlan explicit revision reads that artifact,
null selects latest. Feedback binds a positive plan revision, not an ambiguous
current action ID. Recompute/build commands request owner work; no unchecked publish
port to AIExecution. Feedback does not confirm Vineyard/Winery work.

## Focused contract verification to implement after IO freeze

| ID | Positive / negative checks |
|---|---|
| DEC-C01 | Consume all three unchanged examples via explicit wire mapping; preserve root workspace, opaque batch/source/evidence IDs, exact states, null probabilities and decimal scores. Reject workspace mismatch/extra fields/wrong output schema. |
| DEC-C02 | Every allowed tool/purpose/role accepted; SQL/shell/write tool and unknown enum rejected. Caps at minima/zero cost accepted; token0, timeout999 and negative cost rejected. |
| DEC-C03 | Scalar null/zero/false/string/decimal preserved after source JsonDocument disposal; array/object/Undefined rejected; mutations cannot alter copied facts/evidence/tools. |
| DEC-C04 | Advice OK/INSUFFICIENT_DATA/UNAVAILABLE/POLICY_BLOCKED/ERROR round-trip; numeric successProbability or probability fields rejected. Mismatched request/evidence provenance rejected by future owner validator. |
| DEC-C05 | Probability null differs from valid sourced0; missing method/version/basis or range outside0..1 rejected. All three calibration labels retained; UNVALIDATED never displayed as validated by future presentation. |
| DEC-C06 | Freshness Pending is outside Plan.state; CURRENT guard rejects pending/uncaught-up/revision mismatch/expired or unknown ValidUntil. These constructor tests do not prove DB catch-up/CAS. |
| DEC-C07 | Scope/workspace/horizon/definition guards, bounded non-global audit indication and feedback scope tested; no fake impact/as-of/model pipeline claimed. |

## Actual lightweight evidence and remaining decisions

Read exact schemas/examples and current Contracts/project graph; static audit below
was executed after drafting. Build/DTO/schema-runtime/PG/AI evaluation NOT RUN:
this task is one proposal, not compiled ports. Root owns freeze, independent test
author/reviewer, fresh-main CI and consumer cutover. AIExecution/Decisions owners
acknowledge after assignment. No transferable finding/new skill; reuse existing
wainery-contract-change. No Gate 2 authorization.

IO already approved scope DefinitionVersion and ImpactAnalysisDto coverage flag.
Remaining IO choices: concrete codec placement and configured collection caps;
#27 shared policy type names/dependency order. No budget or scientific decision
is inferred. The audit checks schema consistency only, not handler correctness.

<!-- decisions-audit -->
```python
from pathlib import Path
import json, re
p=Path('agents/handoffs/G1-04-decisions-contract-change.md')
s=p.read_text(encoding='utf-8')
r=json.loads(Path('contracts/tools/agent-advice-request.v1.schema.json').read_text())
a=json.loads(Path('contracts/tools/agent-advice-result.v1.schema.json').read_text())
d=json.loads(Path('contracts/api/decision-plan.v1.schema.json').read_text())
ap=a['properties']; dp=d['properties']; rp=r['properties']
pairs={'AdvicePurpose':rp['purpose'],'AdvisorRole':rp['role'],'AdviceTool':rp['allowedTools']['items'],'AdviceStatus':ap['status'],'EvidenceConfidence':ap['findings']['items']['properties']['confidence'],'ActionType':ap['actionCandidates']['items']['properties']['actionType'],'Feasibility':dp['actions']['items']['properties']['feasibility'],'PlanState':dp['state'],'PredictionStatus':dp['predictions']['items']['properties']['status'],'CalibrationStatus':dp['predictions']['items']['properties']['eventProbability']['oneOf'][0]['properties']['calibrationStatus']}
for name,schema in pairs.items():
    values={x.strip() for x in re.search(r'public enum '+name+r' \{([^}]+)\}',s).group(1).split(',')}
    assert values==set(schema['enum']),name
assert rp['maxInputTokens']['minimum']==rp['maxOutputTokens']['minimum']==1
assert rp['timeoutMs']['minimum']==1000 and rp['maxCostUsd']['minimum']==0
assert 'PENDING_REEVALUATION' not in dp['state']['enum']
assert 'eventProbability' not in ap and 'effectivenessProbability' not in ap
for example,schema in [('agent-advice-request',r),('agent-advice-result',a),('decision-plan',d)]:
    v=json.loads(Path('contracts/examples/'+example+'.json').read_text())
    assert set(schema['required'])<=set(v)<=set(schema['properties'])
assert all((p.parent/link).resolve().is_file() for link in re.findall(r'\]\((\.\.?/[^)#]+)(?:#[^)]*)?\)',s))
assert len(s.splitlines())<=250
print('PASS: ten enum sets, existing budget minima, plan-state boundary, three fixture root shapes and local links (not full JSON Schema validation)')
```

Run this audit from the worktree by extracting the Python block after the
`decisions-audit` marker; actual Git checks: `git diff --cached --check` and
`git diff --cached --name-only` (one allowlisted file). No push/merge performed.
