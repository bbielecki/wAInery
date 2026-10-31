# LLD — Representative business journeys (typed-state walkthroughs)

## 1. Weather changed
New provider response → receipt dedupe → immutable forecast run (`issuedAt`, `retrievedAt`, valid hours) → `WeatherForecastPublished` with changed variables/time window → scoped impact finds frost risk for mapped blocks → local estimator recomputes; advisory only if actual feasible action differs or explanation required. Plan includes when to check local conditions, alternative measure/observe and legal/practical constraints. Without local sensor, confidence limited and action deadline a window rather than invented exact hour. No probability unless forecast/model supplies provenance.

## 2. Winery operation note
User writes 'Zlałem Seyval z osadu wczoraj' → raw text stored privately → proposal extractor identifies candidate batch/date/operation and uncertain clock time; **requires owner confirmation** → Winery writes performed action + outbox atomically → Decisions invalidates only affected batch/process predictions → Context Builder fetches as-of process, confirmed operations, relevant readings, curated knowledge → advisor suggests next measurement or process step with alternatives. Do not infer SO2 dosage without verified measurements.

## 3. New PDF chapter
Private PDF captured, versioned OCR, paragraph chunks, rights class `PRIVATE_COPYRIGHT`, local embeddings/index → index version published → knowledge change scoped to topics/style/variety if known; else low-priority audit. Recommendations using relevant evidence marked for reevaluation when material. External model context excludes book content by default; if only restricted evidence supports recommendation, system asks to approve egress or uses local/rule result with limited claim. No automatic public distribution.

## 4. Duplicate and out-of-order events
Repeated source key returns previous receipt; repeated outbox event ignored by consumer inbox. Observation `observedAt` before last ingested reading but `receivedAt` now changes historical trend as-of now, not as-of old evaluation. Concurrent recompute after more recent receipt fails CAS and requeues. Report distinguishes previous stale plan and new revision.
