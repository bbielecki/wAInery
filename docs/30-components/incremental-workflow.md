# End-to-end incremental recomputation — exact MVP behavior

## Flow (illustrative recorded observation)

```mermaid
sequenceDiagram
    participant U as Local user/import
    participant O as Observations
    participant DB as PostgreSQL (owner + outbox)
    participant R as Outbox worker
    participant D as Decisions
    participant A as AIExecution
    U->>O: RecordObservation(idempotencyKey)
    O->>DB: Tx: observation + ObservationRecorded(outbox)
    DB-->>U: accepted observationId
    R->>DB: Lease next published event
    R->>D: Deliver(eventId,scope,changedFields)
    D->>D: Verify dedupe + calculate dependency impact
    D->>DB: Tx: mark only dependent predictions STALE + enqueue job
    D-->>R: Processed; checkpoint eventId
    R->>DB: Ack delivery
    D->>D: Recompute scoped rule/statistic + compare materiality
    opt Material unresolved decision change
      D->>D: Build as-of context + permitted evidence
      D->>A: AgentAdviceRequest (bounded)
      A-->>D: Typed candidate advice/usage
      D->>D: Validate constraints + rank + persist revision
    end
    D->>DB: Tx: publish prediction/plan revision + outbox
```

## Reliability and consistency

1. Domain transaction saves fact + outbox event atomically (producer at-least-once dispatch). Message metadata carries `eventId`, `correlationId`, `causationId`, subject, source effective/recorded times and aggregate version.
2. Consumer deduplicates by `(consumerId,eventId)` in same transaction that marks impacted predictions stale and creates recompute jobs. Acknowledgement happens after commit. Events may be delivered repeatedly and out of order; projection uses aggregate/source version, not arrival order alone.
3. Coalescing key: `(workspaceId,subjectKind,subjectId,predictionKind,horizon)`. On burst, update highest input revision; ordinary jobs debounce briefly; critical rule lane bypasses debounce. **Never discard intermediate raw measurements.**
4. On execution capture `inputRevision`, `asOf`, `SourceVersionRef[]`. Re-read latest revision before committing; if newer input exists, discard stale output and requeue. One active worker lease per scope.
5. If non-material, only prediction status/freshness and metadata update; LLM call count must be **zero**. If significant change to feasibility, deadline, evidence or risk tier, generate new plan revision.
6. Worker crash -> expired lease reclaimed; bounded retry/dead-letter for poison events. Local host off -> job resumed on next launch. No 24/7 promises.
7. New document indexing does not invalidate *every* prediction, but **cannot be ignored merely because it was never previously cited**. Index publication includes affected tags (topic, variety, location/phase applicability); invalidation uses `PredictionDefinition.knowledgeDependencyTags` and relevant active subjects. If tags are missing/unreliable, enqueue a **bounded, low-priority background audit** of active decision types rather than permanently skip possible newly relevant evidence. A rights change immediately invalidates any plan using restricted evidence, regardless of materiality budget.

## Forecast as-of semantics

At historical evaluation time `T`, select only forecast runs where **both** `issuedAt <= T` and `receivedAt <= T`; select sensor/operations records with observed/effective times <= T AND known-to-system arrival/record time <= T. This prevents hindsight leakage. Current state may use latest run, but reports label unavailable/stale windows.

## Budget ladder

L0 persist and validate locally; L1 deterministic/statistical rules; L2 only selected agent calls if materiality or an uncertain decision demands it. Replay/benchmark can use fake L2. Instrument `llmCallsPerChangedEvent`, tokenCostPerAcceptedPlan, missedCriticalRisk, irrelevantRecomputeCount.

## Safety response

If material and AI unavailable, deterministic checks still run; publish no pretend recommendation, retain previous plan tagged `STALE` and report unable to reassess. Never use uncalibrated model confidence as a probability.
