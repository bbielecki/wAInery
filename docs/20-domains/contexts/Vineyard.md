# Vineyard bounded context

**Class:** Core domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Vineyard/`.

## Purpose
Represent the real agricultural unit and trustworthy history of its management. Specific to Polish/local variety performance and phenology rather than global assumptions.

## Owns
`Vineyard`, `VineyardBlock`, `VarietyPlanting`, `PhenologicalStageAssertion`, `VineyardActivity`, activity amendment/outcome. Block-to-variety assignment and accepted phenological stage remain canonical here.

## Business invariants
- Every activity has a target block/area, effective time, and planned/performed status; never mark performed solely because an agent recommended it.
- Current phenological stage has observation provenance, timestamp and explicit source/acceptance status.
- Avoid per-vine records for MVP; support future row/vine granularity through stable subject hierarchy.
- A second identical import may not create duplicate activity; amendments record correction, reason and original reference.
- A pesticide/treatment recommendation is not an execution authorization; statutory product constraints live in decision validators / official-source adapters when implemented.

## Consumes
Normalized activity commands from Connectivity, approved stage changes from user, observations/query projections from Observations, weather context only via Decisions (not direct DB coupling).

## Publishes (names illustrative)
`VineyardProfileChanged`, `VineyardActivityRecorded`, `PhenologicalStageConfirmed` with `WorkspaceId`, `BlockId`, aggregate version and times.

## Excludes
Sensor readings; forecast provider raw data; AI opinions; recommendation ranking; source-document copyright metadata. These belong to other contexts.

## MVP acceptance
Add block + planted varieties, import dated action, query history, correct note with audit, issue scoped change; zero cross-context table writes. Future field operation types should be additive without redefining the core.
