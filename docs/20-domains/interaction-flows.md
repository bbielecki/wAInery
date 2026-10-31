# Cross-context interaction flows & contracts (conceptual)

Status: Gate 2 accepted. **Event names, payload fields, and APIs are not yet frozen; Gate 3 will produce machine-readable schemas.**

## Ingestion and change processing

```mermaid
sequenceDiagram
 participant Source as Source / user
 participant Con as Connectivity
 participant Owner as Canonical owner (Vineyard/Winery/Weather/Observations/Knowledge)
 participant DB as PostgreSQL outbox/jobs
 participant Dec as Decisions
 participant AI as AIExecution
 Source->>Con: Import / fetch / manual entry
 Con->>Con: Store raw, validate source, idempotency
 Con->>Owner: Typed intake command (not direct DB writes)
 Owner->>Owner: Persist canonical fact + change event atomically
 Owner->>DB: Outbox event in same transaction
 DB-->>Dec: At-least-once delivery
 Dec->>Dec: Determine affected subjects + prediction types
 Dec->>Dec: Mark stale; materiality, dedupe, schedule
 alt Rules / statistics sufficient
   Dec->>Dec: Incremental recompute locally
 else Decision requires interpretation
   Dec->>AI: Scoped facts + allowed evidence references
   AI-->>Dec: Structured candidate findings / tool results
 end
 Dec->>Dec: Validate, rank, store plan revision
```

If AI unavailable, canonical data ingestion is still committed, affected prediction is marked stale/degraded and retry is scheduled (bounded). Fail closed for unsupported safety-critical recommendations.

## Proposed published integration events

| Event family (example) | Published by | Interested consumer | Initial invalidation scope |
|---|---|---|---|
| `VineyardProfileChanged` | Vineyard | Decisions | Affected vineyard block/variety predictions |
| `VineyardActivityRecorded` | Vineyard | Decisions | Block + relevant risk models/planned activities |
| `WineryBatchStateChanged` | Winery | Decisions | Specific batch and linked processes |
| `WineryOperationRecorded` | Winery | Decisions | Specific batch only |
| `ObservationRecorded` | Observations | Decisions | Subject, measurement type and relevant time window |
| `WeatherForecastPublished` | Weather | Decisions | Spatially affected blocks and forecast windows |
| `KnowledgeIndexPublished` | Knowledge | Decisions | Change of evidence version; selective re-evaluation, not global storm |

**Contracts to define in Gate 3:** envelope `eventId`, `type`, `schemaVersion`, `occurredAt`, `recordedAt`, `workspaceId`, `aggregateId`, `aggregateVersion`, `causationId`, `correlationId` and version references where applicable. Enforce idempotency via inbox/dedup on consumers; consider ordering within aggregate, not global ordering.

## Query boundaries and snapshot strategy

Decision Context Builder composes, through owning context query ports:
- Vineyard: approved planted varieties, phenological state, recent activities, constraints.
- Winery: batch history, stage, vessel location and interventions.
- Observations: recent measurements/notes, units, quality, availability, trends.
- Weather: run issued before prediction time; relevant spatial and time interval forecasts.
- Knowledge: evidence search with citations, suitability (Poland/region) and rights filtering.
- AIExecution: execute *approved* model calls; record metrics and return typed findings.

Build a **version-stamped context snapshot** and store IDs/checksums with `PredictionRun`. Never include entire PDF or full sensor history in every prompt. A stable, unchanged snapshot and decision policy may use a cached result. Time-based expiry still invalidates stale weather predictions.

## Event processing at low cost

- PostgreSQL transactional outbox + lease-based jobs. `LISTEN/NOTIFY` optional wake-up only, never only durable queue.
- Material changes: threshold crossing, trajectory deviation, operation committed, data becoming invalid/stale, time window nearing deadline.
- Routine changes: coalesce by `(WorkspaceId, SubjectRef, PredictionKind, Window)` and debounce briefly.
- Critical conditions: deterministic risk-check immediately; LLM never blocks low-level detection.
- Local host offline: persist upstream data only when reachable; on restart catch up and label results with data freshness. It cannot promise real-time alerts while switched off.

## Decisions versus model output

Model returns suggested findings and action candidates; Decisions validates domain constraints, evidence, candidate dependencies, contraindications, action eligibility, timeline and scoring. `ActionEffectivenessProbability` must remain unknown unless supported by calibrated statistical/empirical model; qualitative confidence is distinct. A plan may recommend `MeasureFirst` or `NoIntervention`. Keep alternatives grouped if combinable, rather than selecting exactly one action.

## Data classification

Knowledge enforces source IP metadata and retrieval eligibility. AIExecution enforces data egress rules before any external model call; Decisions and Context Builder cannot bypass them. Logs store hashes/IDs/usage, not copyrighted excerpts or secrets. Governance details to be finalized in Gate 3.
