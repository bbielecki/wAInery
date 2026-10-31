# Winery — component boundary proposal

## Components / ownership
- `BatchRegistry`: WineBatch identity, variety composition if known, status, process stage, batch version.
- `VesselRegistry`: vessel identity/capacity; vessel occupancy with effective time; batch ID independent of vessel ID.
- `CellarOperationJournal`: operations as proposed/confirmed and corrected history with explicit qty/unit; trace transfer, racking, addition, start/stop.
- `WineryReadFacade`: immutable batch context snapshots as-of a date, including relevant vessel history and confirmed operations.

## Public contracts
`CreateWineBatch`, `RegisterVessel`, `AssignBatchToVessel`, `ProposeCellarOperation`, `ConfirmCellarOperation`, `CorrectCellarOperation`, `GetWineryDecisionContext`. For movement or blends, preserve source/target batch and vessel refs and time. MVP supports simple transfer; full batch genealogy later without identity break.

## Events
`WineBatchStateChanged`, `WineryOperationRecorded`, `VesselOccupancyChanged`; produce changed fields and affected batch IDs, not raw diary passages.

## Data/storage
Schema `winery`: `batches`, `vessels`, `occupancies`, `operations`, `operation_revisions`, `outbox`. Invariants: no simultaneous inconsistent occupancy; transfers require a source and timestamp, no AI execution.

## Failure, tests, evolution
Treat missing necessary amounts or destination as `PROPOSED` rather than performed. Two tanks swapping contents must not change WineBatch IDs. Later recipe engine and analyses refer to batch as a SubjectRef, not hard-coded vessel.
