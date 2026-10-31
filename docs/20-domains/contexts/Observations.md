# Observations bounded context

**Class:** Supporting domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Observations/`.

## Purpose
Immutable, typed record of manual measurements, human qualitative observations and future IoT readings, with acquisition quality, units, methods, calibration and subject binding.

## Owns
`MeasurementType` (schema/allowed dimensions and units), `Observation` (numeric or narrative payload with discriminated kind), `ObservationQuality`, instrument/method metadata. Future device calibration refs may be associated without Observations owning device identities.

## Business invariants
- Observed/received times distinct; future predictions never use a measurement ingested later than allowed historical cutoff in tests.
- Numeric measurements have a canonical quantity/unit and traceable original unit; values with incompatible dimensions rejected or quarantined.
- pH, titratable acidity, concentration of individual acids and sugar (Brix) are distinct types; no silent unit equivalence.
- Observation is a fact claimed by a source, **not** automatically accepted phenological stage or operation performed.
- Immutable observation with correction/supersession event, not invisible update.
- `SubjectRef` records which block/batch/vessel measurement applies to, including vessel-batch occupancy at sampled time where known.
- Idempotent ingestion by `(SourceId, ExternalObservationId)` or validated payload hash/fingerprint (with collision/legitimate repeats considered).

## Consumes
Measurements or descriptive observation commands from Connectivity; subject existence/semantics from Vineyard/Winery query ports when needed.

## Publishes (illustrative)
`ObservationRecorded`, `ObservationSuperseded`, `MeasurementQualityChanged` scoped to subject/measurement type/time interval.

## Excludes
Vineyard activities, winery interventions, forecasts, disease prognosis, action selection.

## MVP acceptance
Register pH, Brix, temperature and narrative observations with timestamps/units/source; reject inconsistent units; support future sensor types without schema change (where type compatible); query recent good-quality readings per subject.
