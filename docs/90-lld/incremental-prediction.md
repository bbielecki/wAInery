# LLD — Incremental prediction and refresh scheduling

## Business invariant
Every **accepted canonical update** creates durable evidence and a change event; every affected prediction is evaluated for staleness and possible recompute. It does **not** mean every event invokes an LLM. If the app is off, events remain pending until restart. After restart refresh in order and warn that forecasts may be stale.

## Registry, graph and scope
A `PredictionDefinition` declares `code`, semantic version, supported subject kinds, input dependencies by `(producer,eventType,changedFields)`, topical knowledge tags/varieties, weather-variable windows, feature aggregators, materiality policy, TTL, failure policy, local estimator and optional agent advisory policy. The dependency graph is compiled from definitions and validated at bootstrap, not modeled as a new graph database. Example:

```yaml
code: frost_exposure_v1
subjects: [Vineyard.Block]
inputs:
  - {producer: Weather, event: WeatherForecastPublished, fields: [air_temperature, wind_speed], window: 'P3D'}
  - {producer: Vineyard, event: PhenologicalStageConfirmed, fields: [phase]}
materiality: {strategy: threshold_or_delta, priority: critical}
algorithm: local_rule_v1
advisor: VineyardAdvisor
```

Same forecast runs may affect multiple blocks by proximity/location mapping; no global farm-wide recompute by default. Knowledge index changes cause topic/variety/phase invalidation; unknown tags enqueue a bounded coverage-audit, never silently ignore.

## Detailed algorithm
1. Owner commits canonical change and outbox event in one transaction.
2. Dispatcher runs at-least-once. Inbox dedupe makes consumer behavior idempotent; evaluate impact over changed fields and subject mapping.
3. For each affected scope, upsert `prediction_states`: `input_revision++`, status `STALE` unless already `RECOMPUTING`; coalesce `processing.jobs` by scope key with `desired_revision=GREATEST(old, new)` and high priority for critical changes. Store the provenance of invalidation.
4. `RecomputeWorker` claims ready jobs with `FOR UPDATE SKIP LOCKED`, lease expiry and limited attempts. Load fresh snapshot as-of run start. Cheap L0 validation/feature deltas first, then L1 local deterministic/statistical model.
5. If new risk/action remains unchanged within documented materiality policy, publish refreshed prediction state, **without** LLM, and preserve existing plan revision.
6. If material, missing explanation, or conflicting knowledge: retrieve bounded evidence, authorize egress, choose specialized advisor, request typed action candidates. Validate citations and constraints locally.
7. CAS publish: `UPDATE ... WHERE id=:id AND input_revision=:capturedRevision`; if zero rows, discard stale result, leave last result historical and reschedule latest. Never mark older output as current.
8. Evaluate action feasibility, rank compatible plan, create plan revision only if semantically changed; enqueue report/notification event (local report MVP).
9. Mark job done only if its desired revision matches scope current revision; otherwise set `PENDING`, preserving latest version. On restart requeue expired leases. Retry with jitter, capped attempts, `DEAD` + operator-visible status.

## Concurrency and correctness
A worker can receive 100 identical sensor updates: event dedupe + job key prevents 100 concurrent model calls; latest `input_revision` wins. Add bounded debounce (e.g. 1–5 min) for normal updates, bypass for critical known safety thresholds. No speculative safety alert should wait for paid LLM. `CURRENT` is only valid if impact analysis has caught up to committed relevant events (pending events produce `PENDING_REEVALUATION` in read freshness envelope), evaluation version matches and `valid_until` has not elapsed. Before impact resolution, surface `PENDING_REEVALUATION` globally or by domain scope; this transitional freshness marker does not imply that unrelated predictions have already been invalidated.

## Prediction vs decision recompute
A `PredictionRun` contains model method, input feature snapshot, horizon, event probability nullable with provenance and calibration, severity and data quality. `DecisionPlan` evaluates competing actions; not every numeric risk change requires a new plan or notification. Hysteresis and `semantic_plan_hash` prevent alert noise. Trigger periodic TTL refresh even with no incoming events.

## Negative cases
Missing stale weather -> `INSUFFICIENT_DATA`; provider down -> retain provenance with stale status, no invented certainty. Partial OCR -> no index publication. Future-dated late observation -> store observed/received times, apply as-of semantics. Unrecognized change type -> bounded audit job + visible coverage gap. Invalid model output -> one controlled repair attempt max, then review-needed. Budget exhausted -> keep L0/L1 and explicit lack of AI reasoning; never silently replace expensive model with privacy-incompatible vendor.

## Performance goals (design targets, not measured)
- 0 LLM calls for unrelated changes; at most one active scoped recompute per logical key.
- Typical event recorded transactionally and decision invalidation processed within a few minutes while local host running; critical local risks evaluated promptly.
- DB locks short; model invocation outside DB transaction; memory bounded by paging and max context tokens.
