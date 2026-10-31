# Observations — component boundary proposal

## Components / ownership
- `MeasurementCatalog`: extendable definitions of type, physical dimension, canonical unit, method requirements, bounds and data quality policy.
- `ObservationLedger`: immutable numeric/text asserted observations with `observedAt`, `receivedAt`, source identity, unit and effective subject binding.
- `QualityValidator`: unit compatibility, freshness, duplicate or anomalous value; valid/flagged/quarantined output; no silent normalizing of incompatible units.
- `ObservationReadFacade`: last-N/time-window/as-of metrics with quality indicators; no direct decisions.

## Public contracts
`RecordObservation` accepts a SubjectRef (context, kind, id), type ID, original quantity/unit and method; `CorrectObservation` creates supersession; `QueryObservations(workspace,subject,typeSet,asOf,maxCount)`.

## Events
`ObservationRecorded`, `ObservationCorrected` scoped to subject and measurement type; include observation ID and time window so Decisions can filter.

## Data/storage
Schema `observations`: `measurement_types`, `observations`, `supersessions`, `outbox`; initial indexes `(workspace_id,subject_kind,subject_id,type,observed_at)`; store value in standardized numeric/decimal for numeric measurements, and narrative separately. `pH` != titratable acidity != Brix.

## Failure, tests, evolution
Reject incompatible unit conversions. Preserve source value and canonical value independently. Never infer an action completed from a narrative observation. IoT ingest later maps to same observation API; add partitions only when volumes justify.
