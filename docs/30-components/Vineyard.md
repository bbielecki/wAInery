# Vineyard — component boundary proposal

## Components / ownership
- `ProfileRegistry`: Vineyard, VineyardBlock, VarietyPlanting; stable IDs and optional regional attributes; scope at block in MVP.
- `PhenologyState`: confirmed developmental stage with method, observedAt, provenance, correction/version; never infer confirmed stage from LLM.
- `ActivityJournal`: planned/performed activity timeline and append-only revisions, idempotent acceptance of a draft operation.
- `VineyardReadFacade`: snapshots for Decisions (block, varieties, confirmed stage, recent activities) as-of time.

## Public command/query contracts
`RegisterBlock`, `UpdatePlantings`, `ConfirmPhenologicalStage`, `ProposeVineyardActivity`, `ConfirmVineyardActivity`, `CorrectVineyardActivity`; `GetVineyardDecisionContext(workspaceId,blockId,asOfUtc)`.

Command result: stable ID, canonical version, status `PROPOSED|CONFIRMED|REJECTED`, rejected fields with machine-readable errors. **Only explicit confirmation records a performed action.**

## Events
`VineyardProfileChanged`, `PhenologicalStageConfirmed`, `VineyardActivityRecorded` -> Decisions impact routing. Each event includes subject, effective time, owner aggregate/version and changed field tokens, not full free text.

## Data/storage
Schema `vineyard`: `vineyards`, `blocks`, `plantings`, `phenology_assertions`, `activities`, `activity_revisions`, `outbox`. `WorkspaceId` present; JSONB only for supplementary metadata.

## Failure, tests, evolution
Concurrent duplicate note import -> same canonical activity via source idempotency. Operation correction -> new revision and new impact event, not silent overwrite. Stage/variety changes target only affected blocks. Do **not** add per-vine structure until required; stable subject namespace permits it later.
