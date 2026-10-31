# Decisions — component boundary proposal (core value path)

## Components / ownership
- `ChangeImpactAnalyzer`: deterministic dependency registry maps typed source event + subject + fields + time-window + knowledge topic/variety tags -> affected prediction definitions/scope. **Never global invalidation by default.** Unclassified knowledge changes use a bounded coverage-audit job (do not silently ignore newly relevant knowledge).
- `PredictionRegistry`: definition/method/version, input fingerprint and revision, status `CURRENT|STALE|RECOMPUTING|FAILED|INSUFFICIENT_DATA`, validity horizon, probability metadata.
- `RecomputeScheduler`: persisted scoped jobs (key = workspace + predictionType + subject + horizon), bounded coalescing for normal changes, immediate lane for safety-critical changes; one pending job per scope; recover/lease on restart.
- `ContextAssembler`: use owner read facades, rights-filtered evidence and as-of time. Build minimal context and evidence refs, not raw full document or time series.
- `ActionEvaluator`: candidate constraints, prerequisite/exclusivity groups, feasibility, urgency, expected benefit/loss and unknown-risk handling. Does **not** turn LLM preference into probability.
- `DecisionPlanner`: choose single best feasible action *or compatible plan of multiple actions*, include measure-first/no-action options and versioned alternatives. Persist plan + revisions.
- `DecisionReadFacade`: local report/CLI/HTTP snapshot (plan, evidence, uncertainties, invalidation/revision reason).

## Public contracts
`OnDomainFactChanged`, `GetPrediction(workspace,predictionType,subject,horizon)`, `RecomputePrediction(scope,reason,asOf)`, `BuildDecisionPlan(scope,asOf)`, `GetDecisionPlan(planId)`, `RecordDecisionFeedback(planId,actionId,feedback)`; call provider-neutral `IReasoningExecutor` for `AgentAdviceRequest` / `AgentAdviceResult` only **after** materiality check.

## Events
`PredictionInvalidated`, `PredictionUpdated`, `DecisionPlanPublished`, `DecisionPlanSuperseded` (no raw text). Consumers later implement Notifications via event subscription.

## Data/storage
Schema `decisions`: `prediction_definitions`, `prediction_states`, `prediction_runs`, `invalidation_reasons`, `decision_plans`, `plan_revisions`, `action_candidates`, `feedback`, `outbox`; queue/job infrastructure in shared application schema `processing` with an integration owner.

## Critical invariants/tests
- Acceptance of any owner event transactionally marks affected predictions stale **before** queuing work. No changed event -> unrelated AI calls. Job coalesces by logical key, but latest input revision rechecked before publishing plan; lost updates cause immediate requeue.
- A prediction probability has `value`, `method`, `modelVersion`, `calibrationStatus`, `dataBasis`; if missing, no numeric %. Evidence confidence is separate categorical enum.
- Plan can comprise multiple compatible actions; ranking includes safe baseline `OBSERVE|MEASURE|NO_ACTION|ESCALATE` when applicable. Constraints can veto, not just reduce a numeric score.
- Context snapshot stores source IDs + versions, access policy outcome, as-of + token footprint. LLM results are drafts and require schema + rule validation; if fail, publish `INSUFFICIENT_DATA/FAILED` rather than plausible fiction.
- No numerical forecast from a pure LLM vote. No direct domain write or automatic physical action.
