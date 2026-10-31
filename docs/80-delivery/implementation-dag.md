# Parallel implementation DAG — planned agent work packages

**Gate 3 proposal**, not permission to implement before approval. Independent agents own module internal paths; integration owner owns shared contracts, migrations bootstrap, host and CI. Preserve small fixed cost and quality-first scope.

```mermaid
flowchart TD
    I[Integrator: base contracts / migrations / host shell] --> C[Connectivity]
    I --> V[Vineyard]
    I --> W[Winery]
    I --> O[Observations]
    I --> WX[Weather]
    I --> K[Knowledge]
    I --> AI[AIExecution]
    V & W & O & WX & K & AI --> D[Decisions: context / incremental / ranking]
    C & D --> INTEG[Integrator: 3 vertical journeys]
    INTEG --> EVAL[QA/Evaluator: 30-50 scenarios, costs]
    EVAL --> REVIEW[Architecture + domain safety review]
```

## Implementation order and seams

**Wave 0 — Integration owner (serial, short)**: skeleton .NET solution, `BuildingBlocks.Contracts`, agreed schemas/event envelope, shared fixture generator, PostgreSQL bootstrap and local compose. Agent assignments go active after schema version freeze. Source-level graph: domain modules can import BuildingBlocks and others' contract assemblies; never other's implementation. AIExecution implements Decisions-owned neutral port through Host DI.

**Wave 1 — Parallel (independent)**: Vineyard, Winery, Observations, Weather, Knowledge, Connectivity, AIExecution. Each writes only `src/Modules/<name>/**`, `tests/Modules/<name>/**`, module-specific LLD notes, and agent handoff. Changes to shared contracts require integration-owner PR; no simultaneous shared-file edits. Connectivity adapters can mock target command ports while canonical owners are still in development.

**Wave 2 — Decisions**: after stable owner read DTOs and `IReasoningExecutor`, implement impact, prediction states, coalescing/recompute, context assembly, ranking, as-of snapshots, plan persistence. Prototype/mocks allowed in parallel early, integrate after Wave 1 contracts.

**Wave 3 — Integrator**: wire ingestion → canonical writes → outbox → Predictions → limited AI → plan report. Test three journeys, crashes/replays, stale forecast, no irrelevant LLM calls, budget caps, rights enforcement.

**Wave 4 — QA and independent reviews**: expert-reviewed scenarios, retrieval/citation quality, Polish relevance, ranking feasibility, probability provenance, failure safety, and measured total API cost. Reviewer is separate from module implementation owner where feasible.

## File ownership and conflict policy

- `contracts/**`, `src/Host/**`, `src/BuildingBlocks/**`, `infra/**`, `.github/**`, central migration runner: **Integration Owner**.
- `src/Modules/<module>/**`, `tests/Modules/<module>/**`: **that module agent**.
- `tests/Contracts/**`, `tests/Architecture/**`, `tests/Integration/**`, `tests/Evaluation/**`: assigned QA/Integrator; bounded PR ownership.
- Module agent proposes contract additions in `agents/handoffs/<agent>-contract-change.md`, does not modify shared files itself.
- No agent imports `src/Modules/OtherModule` internals or uses EF entity classes across boundaries.

## Completion criteria for each package

README scope, interface/DTO with version, consumer example, failure & idempotency semantics, migration, tests, measured cost impact, no secrets/protected contents, and handoff note. This gate documents **what** to build; Gate 4 finalizes **how**, including all DB DDL, class diagrams, algorithms, edge-case tests and provider adapters.
