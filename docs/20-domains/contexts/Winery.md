# Winery bounded context

**Class:** Core domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Winery/`.

## Purpose
Maintain trustworthy cellar production state and history from must to wine, independent of hardware connectivity or AI inference.

## Owns
`WineBatch`, `Vessel`, `VesselOccupancy` (or equivalent), `ProcessStage`, `WineryOperation`, batch genealogy / traceable transfer reference. Four 200 L stainless tanks are initial vessels, but batch ID is stable across transfers.

## Business invariants
- `WineBatch` is not `Tank`; contents can transfer, mix, split. Traceability of source batch and time is mandatory for a recorded transfer.
- Planned, advised and executed operation states are distinct; only verified execution changes production history.
- Each operation records date/time, who confirmed, quantities/units if applicable, and outcome or reason for missing values.
- No AI writes directly to production state. Normalized description from natural language is proposed, not silently authoritative when critical attributes are missing.
- Do not model all enological processes as untyped arbitrary JSON; use structured operation kinds with extension metadata where appropriate.

## Consumes
Validated commands from user / Connectivity; measurements queried through Observations (pH, Brix, acid, temperature); no private table reads from other contexts.

## Publishes (illustrative)
`WineBatchStateChanged`, `WineryOperationRecorded`, `VesselOccupancyChanged` with affected batch/vessel/time for recomputation.

## Excludes
Raw sensor time series, external weather forecast, LLM-generated recommendations, book evidence index.

## MVP acceptance
Create batch and vessel; record fermentation start and activity; read batch stage and operations; register batch moved between vessels without conflating identity; decisions scoped to batch.
