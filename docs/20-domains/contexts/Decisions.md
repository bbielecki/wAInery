# Decisions bounded context

**Class:** Core domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Decisions/`.

## Purpose
Continuously maintain versioned, evidence-backed predictions, competing viable actions and a justified plan of what to do and when. Maintain domain truth that **recommendation is not execution**.

## Owns
`PredictionDefinition`, `PredictionRun`, `Prediction`/`RiskAssessment`, `AffectedScope`, `ActionCandidate`, `DecisionPlan`, `DecisionPlanRevision`, `DecisionFeedback`, input snapshot/version references, local materiality and ranking policies.

## Business invariants
- Every prediction has subject, horizon/validity interval, current/stale/failed/insufficient state and versioned input evidence references.
- Raw new data impacts only dependent predictions (via source type + subject + time range map); stale marking precedes async recomputation.
- Predictions may use deterministic rules, statistical model or approved AI analysis; chosen method, version and uncertainty explicit.
- Never invent success probabilities; `Probability` optional and includes model provenance/calibration state. Qualitative evidence confidence kept separately.
- Alternatives may be mutually exclusive, combinable or prerequisite actions; no default single-choice assumption.
- A plan may select `MeasureFirst`, `NoAction`, or `Escalate` when evidence insufficient; ethical/physical constraints checked outside LLM.
- Old decision plan and input versions retained for reproducibility; feedback is not proof of an executed operation in Vineyard/Winery.

## Consumes
Typed domain events from Vineyard/Winery/Observations/Weather/Knowledge. Scoped read queries from their owners; typed AIExecution output through port.

## Publishes (illustrative)
`DecisionPlanPublished`, `DecisionPlanSuperseded`, `PredictionStalenessChanged`, `DecisionFeedbackRecorded`.

## Excludes
Raw source import, weather forecasting provider implementation, LLM SDK, physical action execution, notification delivery.

## MVP acceptance
Three vertical journeys: scoped invalidation + recompute, winery choice with alternatives/unknown probabilities, time-aware weather update; no irrelevant AI calls; plan audit with citations and input versions; evidence-based actionable next-step report.
