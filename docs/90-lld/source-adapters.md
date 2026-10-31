# LLD — Source adapters, observations and weather provider

## Source contract
`ISourceAdapter`: `Discover/FetchAsync(Cursor, CancellationToken)` returns raw bytes + source metadata; `INormalizer<T>` converts a source-versioned payload into canonical proposal, observation or forecast **without writing owner tables**. `Connectivity` owns receipts, content hash, cursors and failed raw payloads; owner modules validate and persist canonical facts. Each adapter declares `SourceCapabilities` (poll/manual/push; freshness; expected units; schema version; rights/license; authentication method).

## Narrative actions: strict confirmation
Raw note from user (including date and target) → source receipt → LLM-assisted extraction **draft only** (`operation type`, `subject`, `date`, `parameters`, `confidence`, `missingFields`) → confirm in CLI/local API → Vineyard/Winery owner command transaction → event. Ambiguous identification, timing, batch/vessel mapping or treatment quantity blocks automatic confirmation. Allow corrected notes without destroying original. No destructive write based solely on model inference.

## Weather: minimum-cost adapter
MVP `OpenMeteoForecastAdapter`: configured lat/lon, timezone-aware UI but UTC in storage, forecast valid timestamps, provider issued/retrieved times (issued may be unavailable; store NULL `issued_at` with `issued_at_basis=UNKNOWN` (or provider-confirmed `issued_at` with basis `PROVIDER`); never invent an issue timestamp), metrics initially air temp, precipitation, wind and relative humidity. Poll e.g. twice/day when host on; HTTP ETag/response hash for no-op; provider outage -> stale forecast flag, bounded exponential backoff, local last known forecast with visible freshness warning. Licence check: free tier requires eligible noncommercial usage; if this winery is commercial, plan licensed provider or eligible public agency data instead. Model mapping must be independently tested against sample provider JSON; schema change quarantined. No paid weather API mandatory.

Provider new run is immutable. `ForecastDiff` compares relevant weather **windows** and materiality by prediction definitions; not all changed weather fields warrant a new AI call. Historical backtest uses forecast *available at the time*, not actual outcomes or retrospectively improved forecast.

## Observation ledger and extensibility
`measurement_types` define value kind, canonical unit, allowed subject kinds, validation range, method and version. External `sourceId` and `subject.id` resolve to internal UUID through authoritative Registry/owner ports; unknown or ambiguous values are quarantined. Intake quality `UNCHECKED`/`FLAGGED` maps to stored `SUSPECT` until validated; `QUARANTINED` never reaches the canonical ledger. A measurement records source ID, external idempotency key, observed/received times, quality and calibration. MVP manual textual or numeric observations through same record contract. On IoT phase adapters consume MQTT/HTTP and add `SensorDevice`; new measurement type is registry entry, not core aggregate migration. Unit conversion is explicit and versioned, e.g. no pH ↔ acidity conversion, density → Brix requires temperature/method where relevant.

## Scope isolation
A wine temperature sensor may attach physically to a vessel; Observations records device/vessel and the `BatchId` resolved via occupancy interval at **observedAt**, not current vessel occupancy. If ambiguous or conflicting occupancy, mark unassigned/untrusted for confirmation rather than corrupt batch history. This is a high-priority integration test for future sensors.

## Failures and replay
Input from unknown source, duplicate provider event, malformed numeric/unit, impossible timestamp, untrusted URL, API 429, failed OCR, wrong timezone → quarantine/retry with reason; raw payload retained if permitted. Idempotency key `(workspaceId,sourceId,externalKey)`; no duplicate domain writes. Replay uses pinned adapter and mapping version, emits new corrective facts, never silently rewrites canonical history.
