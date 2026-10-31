# Gate 2 domain design review (self-review)

Date: 2026-10-31. **Status: accepted by user; historical review findings retained.** Reviewer: architecture consistency pass; **not** a code, security audit or independent domain-expert certification.

## Findings and resolution

| ID | Severity | Finding | Resolution in proposal |
|---|---|---|---|
| G2-01 | High | Ambiguous ownership of descriptive notes vs performed operations | Original note in Connectivity; canonical operation in Vineyard/Winery after validated command; plain observation in Observations |
| G2-02 | High | Sensor observation and WeatherForecast both modeled as `Measurement` | Forecast runs exclusively in Weather; on-site actual measurements in Observations |
| G2-03 | High | Batch/tank coupling would corrupt history on transfers | Batch and Vessel are separate Winery entities; capture vessel-batch association at observation time |
| G2-04 | High | AI-generated recommendations could mutate production truth | AIExecution returns candidates; Decisions validates; Vineyard/Winery only commit actual performed actions |
| G2-05 | High | Private book content could leak via external provider or log | Knowledge rights enforcement, AIExecution egress checks and metadata-only tracing |
| G2-06 | Medium | Full recompute storm on forecast/knowledge updates | Scoped invalidation, coalescing, affected-subject map; explicit materiality policy |
| G2-07 | Medium | Arbitrary numeric probability from language model | Probability nullable with calibration/provenance; qualitative evidence separate |
| G2-08 | Medium | Decomposing tiny MVP into microservices increases cost | One host, one Postgres, separate schemas & contracts only |
| G2-09 | Medium | Evaluation treated as permanent runtime domain | Offline harness/test suite; no extra runtime module now |
| G2-10 | Medium | Source of truth ambiguous if read projections duplicated | Ownership matrix: canonical writes single owner, rebuildable projections only |
| G2-11 | Medium | Domain and AI framework entangled | AIExecution separated and constrained by model-neutral port |
| G2-12 | Medium | Delayed sync or replay leads hindsight bias in backtesting | issuedAt / observedAt / receivedAt + as-of snapshots, versioned source runs |
| G2-13 | Medium | Ambiguous ownership of original PDF bytes between Connectivity and Knowledge | Connectivity owns upload receipt; Knowledge owns accepted canonical document bytes and extraction lineage |
| G2-14 | Low | Weather provider registered in both integration and weather tables | Connectivity owns endpoint/credentials/checkpoint; Weather owns semantic forecast provider identity and issued runs |

## Gate 2 decisions (approved by user)

1. Are `Vineyard` and `Winery` fully separate core domains? Proposed: **yes**, distinct lifecycles and actions.
2. Should `Observations` own numeric + narrative evidence across both? Proposed: **yes**, only raw claimed observations; domain-owned accepted state remains elsewhere.
3. Should `Connectivity` and `AIExecution` be separate logical platform modules in MVP? Proposed: **yes**, lightweight directories within the monolith, not services.
4. Is `Decisions` one core context that owns both predictions and action planning/ranking? Proposed: **yes** initially; split only if complexity/scale demonstrates benefit.
5. Should `Notifications` be modeled as a deferred module rather than built now? Proposed: **yes**; outputs are stored plans and local reports in MVP.

## Explicit deferred validation (Gate 3 and full LLD)

- Contract payload design/schema and semantic versioning rules.
- Invariants, aggregate boundaries, DB schema and indexes.
- Detailed tool ACL/authorization, rights/retention, provider terms and threat model.
- Comparative benchmark: 30–50 scenarios and evidence grounding, costs, calibration and Polish agronomic suitability.
- Implementation/test dependency DAG and parallel agent job ownership.
- Existing GitHub contents and branch/PR deployment once private repository permissions available.

**Resolution:** User approved the proposed eight contexts and unified Decisions ownership. Gate 3 proposes component contracts; schemas are not yet frozen.
