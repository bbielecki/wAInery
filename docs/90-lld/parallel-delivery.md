# LLD — Independent implementation agents, order and handoffs

## Freeze boundaries and workstreams
**Integration owner / Wave 0:** `contracts/**`, `src/BuildingBlocks/**`, `src/Host/**`, `infra/local/**`, migration bootstrap + `tests/Contracts/**`. Create shared DTOs/interfaces using [ports](ports-and-compatibility.md), confirm compilation, JSON examples and DB extension bootstrap. No domain logic in Host.

**Parallel Wave 1**, bounded and independently testable:

| Agent | Owns | Required contract | First test |
|---|---|---|---|
| Vineyard | `src/Modules/Vineyard/**`, `tests/Modules/Vineyard/**` | `IVineyardReadPort`, `ConfirmOperation`, `DomainChange` | confirmed operation + same-transaction outbox |
| Winery | `src/Modules/Winery/**`, `tests/Modules/Winery/**` | `IWineryReadPort`, batch/vessel mapping | transfer preserves batch identity and time |
| Observations | `src/Modules/Observations/**` and tests | `IObservationReadPort`, Measurement registry | unit/type check, idempotency and correction |
| Weather | `src/Modules/Weather/**` and tests | `IWeatherReadPort`, `PublishForecastRun` | immutable runs + as-of selection |
| Knowledge | `src/Modules/Knowledge/**` and tests | `IKnowledgeRetriever`, `IObjectStorage` | citation resolves document + rights filter |
| Connectivity | `src/Modules/Connectivity/**` and tests | `SourceReceipt`, proposals, source ports | narrative cannot write actual operation |
| AIExecution | `src/Modules/AIExecution/**` and tests | `IReasoningExecutor`, `IModelGateway` | forbidden egress; max cost enforced |

**Wave 2** Decisions: `src/Modules/Decisions/**`, tests. Implement dependency registry, transactional invalidation, lease worker, CAS publish, bounded context, ranking and local report using mocks first. Decisions can prototype in parallel but full integration after Wave 0/1 owner ports freeze.

**Wave 3** Integrator: End-to-end scenario path and migration runner; register module DI and configure local startup. **Wave 4** Independent QA + threat review + benchmark; fix findings via respective owner, merge with tests. Public cloud/IoT/UI separate future increments.

## Branch/commit policy
Branch convention `agent/<module>/<scope>`; short commits (contract, persistence, features, tests), PR into integration branch after contract, architecture, integration validation. One agent per bounded folder. File ownership enforced by PR diff check. Shared contract changes proposed in `agents/handoffs`, reviewed and applied by Integration Owner as distinct commit. No cherry-pick of conflicting shared edits.

## Definition of Done for a module
Owner contract + errors/idempotency + tests + migration + sample payload + cost/latency implications + audit/recovery semantics + handoff. Record input/output dependencies, public types, version and known limitations. No private content committed; no unreviewed changes to upstream module internals.

## Pairing of roles to modules
Vineyard/Winery experts are *domain processes* while `VineyardAdvisor`/`WinemakingAdvisor` belong to `AIExecution` (prompts/tool configuration). `Decisions` remains the only publisher of ranking/plan. New specialized advisor adds role/config and approved tools; does not require its own service or database.

## Traceability and acceptance
Each feature/story points to a business requirement (`BR-01..10` in [traceability](../10-architecture/business-requirements-traceability.md)), a contract, one module owner, relevant DDL table and an acceptance scenario. Independent reviewer checks this graph and records changes in review log. Do not claim functional MVP completed after only markdown or SQL files exist.
