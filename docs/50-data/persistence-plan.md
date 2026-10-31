# Persistence & evolution plan — MVP-to-scale

**MVP**: one local PostgreSQL + pgvector, a single application process, local private file root. A migration is acceptable when additive/cheap; no cross-context mutable tables.

## Schema ownership

- `vineyard`, `winery`, `observations`, `weather`, `knowledge`, `connectivity`, `decisions`, `ai_execution`: one canonical writer per schema, own EF context/migrations (or discrete SQL migrations for special extension indexes).
- `processing` (integration-owned) for durable outbox dispatch, consumer dedupe (`processed_events`) and scoped job leases. Keep owner-domain outbox write **in same DB transaction as changed aggregate**. A common physical outbox table is acceptable only if this atomicity and schema ownership are demonstrable; no separate server/broker.
- `workspace_id` on canonical data, already for local single workspace; **not** a claim of full tenant isolation.

## Important index choices

- Observations: `(workspace_id, subject_kind, subject_id, measurement_type, observed_at desc)`; optional received-at for as-of; partition only at measured need.
- Weather: `(workspace_id, location_id, issued_at desc)` and `(forecast_run_id, valid_at)`; runs immutable.
- Decisions: unique active scope `(workspace_id, prediction_kind, subject_kind, subject_id, horizon_key)`; `input_revision` and `context_fingerprint` for invalidation; persisted job unique key + lease expiry.
- Knowledge: index on `(workspace_id, document_version_id, index_version)`; `tsvector`+`pgvector` for hybrid, exact vector search fine at 1–2 books. Version embeddings by model + dimensions; never silently mix vector spaces.
- Domain operation idempotency: scoped source key or unique `(workspace_id,source_id,external_event_id)`; corrections append history.

## Files and data lifecycle

- `IObjectStorage` local implementation stores content-addressed files under one protected root with path traversal protection; `DocumentVault` owns accepted originals, Connectivity owns intake receipts.
- OCR output and vector indices are derived and rebuildable; original books and user descriptions remain durable and private. No raw book content, derived snippets or embeddings in source control.
- Persist `effectiveAt`, `observedAt`, `receivedAt` and forecast `issuedAt` separately; record ingestion/mapping versions and provenance.
- Backup covers DB **and** canonical files. Restoration tested by replaying a sample scenario; failed restore is a gate failure.

## Planned changes and migration cost

| Next step | Schema impact |
|---|---|
| New observation kind, instrument or source adapter | Registry/config data, usually no schema migration |
| More books and web sources | More rows + index versions, no redesign |
| More AI vendors or UIs | Adapter/client, no domain schema migration |
| Time-series IoT volume | Add partitions/aggregation, index strategy or external store by migration |
| Email/mobile notifications | New notification schema + consumer on plan event |
| Multi-tenant SaaS | Real tenant isolation, RLS/policies/permissions, infra migration, not simply adding workspace field |
| Domain concept not anticipated | Small additive EF migration allowed |

## Explicit LLD uncertainties to test

- Use module-specific EF migrations versus integrated migration orchestration to ensure a single owner can bootstrap locally without cross-context deadlocks.
- Choose JSONB for optional metadata only; typed fields for quantities, provenance/time, status and relationships.
- Basic forecast retention and observation growth estimates after real samples; do not prematurely use Timescale/Kafka.
