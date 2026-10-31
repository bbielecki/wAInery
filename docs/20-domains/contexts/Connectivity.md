# Connectivity / Sources supporting platform boundary

**Class:** Generic/platform. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Connectivity/`.

## Purpose
Make future source additions cheap through an adapter registry and deterministic import/retry/replay while avoiding coupling raw payloads to domain aggregates.

## Owns
`SourceRegistration`, `ImportRun`, `RawEnvelope`, `SyncCheckpoint`, mapping version, ingestion failures/quarantine and replay metadata; original manual text receipt. Intake payload may be spooled to local object storage, with DB location pointer and checksums. After a document is accepted, Knowledge owns its canonical bytes and extraction lifecycle; Connectivity retains the receipt, not a second authoritative document record.

## Invariants
- A source cannot write tables of another context; map to typed commands or publish accepted ingestion events.
- Duplicate receives must not duplicate canonical downstream operations; record idempotency keys and accepted/rejected outcomes.
- Never delete original raw data before the documented retention period; sanitize secrets and copyrighted text in logs.
- Failed parsing/quarantined payload remains traceable; errors do not silently convert into partial valid facts.
- Source schema versions and mapping versions independently tracked; adding new sensor of known kind should be configuration, not domain migration.
- No costly generalized ETL platform in MVP; import jobs are local and on-demand or scheduled.

## Consumes
PDF imports, curated URLs, human-entered activity descriptions, free weather API. Future IoT MQTT/HTTP streams.

## Publishes / dispatches
Typed intake requests to canonical owner: Knowledge, Vineyard, Winery, Observations or Weather. No general `AllDataChanged` event; downstream owner publishes meaningful domain event.

## Excludes
Domain semantics of spray/fermentation, approved predictions, book rights ownership, forecast facts.

## MVP acceptance
Register 3–4 source types, store/verify raw inputs, retries with backoff, idempotent imports, recover on restart, and hand off parsed entities without direct module DB writes.
