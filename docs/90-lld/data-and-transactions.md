# LLD — Data ownership, persistence and transactions

## Schemas and aggregates
Reference DDL: [001_foundation.sql](../50-data/sql/001_foundation.sql). The MVP uses a PostgreSQL server with schemas `registry`, `connectivity`, `vineyard`, `winery`, `observations`, `weather`, `knowledge`, `decisions`, `ai_execution`, `processing`. One module owns each, and `processing` is owned by the integrator. No cross-module writes except an owner's atomic transaction appending an outbox row. Use a single DB role only for local bootstrap; production-ready design grants per-schema write rights.

| Aggregate | Owner | Invariants | Mutation |
|---|---|---|---|
| Block, Phenology, VineyardOperation | Vineyard | verified executed operation, explicit effective/recorded time, correction chain | `ConfirmVineyardOperation` |
| WineBatch, Vessel, Occupancy, WineryOperation | Winery | measured batch is independent of vessel; no implicit continuity across transfers | `ConfirmWineryOperation` |
| Measurement | Observations | one typed value, unit, method, quality, provenance, corrections append-only | `RecordObservation` |
| ForecastRun | Weather | immutable issued/retrieved/valid times, version + location | `PublishForecastRun` |
| DocumentVersion + IndexPublication | Knowledge | original private, versioned parsing/rights, publish atomically after complete index | `PublishKnowledgeIndex` |
| SourceReceipt + OperationProposal | Connectivity | idempotent external key, no unconfirmed action as fact | `AcceptImport`, `ConfirmProposal` |
| PredictionState, DecisionPlan | Decisions | versioned dependency manifest; no LLM-reported numeric probability; alternative actions | `Recompute`, `PublishDecisionPlan` |

`SubjectRef` = `(workspaceId, owningModule, subjectKind, subjectId)`, where the id is UUID in canonical stores. Cross-context references **are validated by owner ports**, not cross-schema foreign keys. `workspaceId` always travels with queries. The workspace column is future-proofing, **not** current tenant isolation.

## Time semantics (non-negotiable)
- `effective_at` / `performed_at`: business occurrence; `observed_at`: measurement sampling; `issued_at`: weather provider forecast generation; `valid_at`: time being forecast; `received_at`: when our system received data; `as_of`: cutoff of data **known to our system** for replay/evaluation.
- For historical evaluation use `received_at <= asOf` *and* `issued_at <= asOf` for weather; NEVER select final observed weather or newer forecasts as though known at prediction time.
- Correction is a new record referring to the corrected record; do not rewrite history. Queries choose latest valid correction as-of. For future instrument metadata, store calibration version and reliability quality.

## Transaction boundary
Example owner command (`ConfirmWineryOperation`): validate idempotency + target → insert operation → increment batch version → insert `processing.outbox` event with opaque `data_ref`, same PostgreSQL transaction → commit. A committed operation with missing event is a correctness defect. A crash after commit but before dispatch is safe because the outbox persists.

Consumer transaction: acquire bounded outbox batch with `FOR UPDATE SKIP LOCKED`; insert `(consumer,event_id)` into `processing.inbox_dedupe`; update `decisions.prediction_states` or `processing.jobs`; commit; mark outbox delivered **only after all registered consumers acknowledge**. A single `delivered_at` column is sufficient for MVP **only with exactly one dispatcher consumer**, which fans out sequentially to subscribers under persisted transaction/dedupe. Adding more durable consumers requires per-consumer offsets/delivery records, not premature marking.

**Read-after-write freshness guarantee**: outbox propagation is eventually consistent. API responses immediately after a write must display `PENDING_REEVALUATION` (or `FRESHNESS_UNKNOWN`) using a persisted workspace change watermark until scoped impact processing catches up; do NOT present old `CURRENT` plan as settled. Once impact computed, unrelated predictions remain current, affected predictions become stale and scheduled. No fake strong consistency claim across module commit and async work.

## Initial indexes and scaling
`measurements` has `(workspace, subject, type, observed_at DESC)`. `forecast_runs` `(workspace, location, issued_at DESC)`, `(forecast_run_id,valid_at)` for points. One active prediction scope row with unique `(workspace,type,subject,horizon)`. Knowledge `tsvector` plus exact pgvector distance; a one-to-one `(chunk, embedding_space)` index. `vector` is intentionally dimension-unconstrained to permit parallel migration of embedding versions: dimension equality is validated in the embedding service and DB query. At scale add dimension-specific ANN indexes after benchmarking. No early TimescaleDB, graph DB, or data lake.

## Backup and migration
Canonical originals under protected content-addressed root `SHA256` with metadata in DB; canonical operations, source receipts, forecast runs and knowledge provenance preserved. OCR and embeddings rebuildable. Backup DB and raw files together; run a restore drill. Additive migrations are acceptable; no `ALTER ... DROP` of canonical history without signed-off migration plan. Keep pinned migration order and schema version. Never check actual book text, scanned pages, raw narratives or embedding binaries into Git.

## Known SQL-blueprint caveats
The DDL is a review artifact for module agents, not a completed EF migration. Before use, add constraint enforcement for overlapping vessel occupancies (consider exclusion constraints on range), unit canonicalization, numeric precision and source ID owner resolution; enforce workspace equality on cross-ref corrections; resolve schema-specific migration ownership. These are blocking implementation tests, not assumed correct because the SQL file exists.
