# wAInery — Smart Winery AI

**Product:** local-first decision intelligence for growing grapes and making wine in Poland.  
**Status:** Gates 1–3 (layout, domains, components) approved; Gate 4 LLD documented and architecture-reviewed, **not yet implemented or scientifically validated**. See the [business HLD](docs/10-architecture/business-hld.md) and [architecture assessment](docs/70-quality/architecture-review-2026-11.md).

## Business HLD — what the product must do

For a specific **vineyard block or wine batch**, wAInery recommends **what to do next, when and why**. It should compare **multiple feasible actions**, including measure-first, wait or do nothing, explain dependencies/risks and cite applicable evidence. It must account for the vineyard's **Polish microclimate, varieties, phenology and real operational history**, not rely primarily on generic book advice.

**MVP inputs:** 1–2 privately scanned books, curated online sources, owner-written descriptions of vineyard/cellar activities and observations, one free forecast API.  
**MVP output:** a persisted, versioned action plan and local report with priority, time window, alternatives, rationale, missing data, evidence and qualitative uncertainty. **Percentages appear only when justified by an independently assessed model.**

```mermaid
flowchart LR
    Source["Accepted data change"] --> Facts["Domain-owned facts + outbox"]
    Facts --> Scope["Incremental impact / stale scopes"]
    Scope --> Rules["Local risk models & materiality"]
    Rules --> Decision{"Advice needed?"}
    Decision -->|No| Plan["Versioned feasible action plan"]
    Decision -->|Yes| Evidence["Scoped context, RAG, specialist advisor"]
    Evidence --> Plan
    Plan --> Report["Local report + human feedback"]
```

Every accepted change must lead to **dependent** prediction assessment; it must **not** trigger all models/agents. The owner confirms actual work; an AI suggestion never becomes a performed operation automatically. The offline/local MVP does not guarantee alerts while the computer is off.

## Architecture at a glance

| Core bounded contexts | Supporting contexts | Platform modules |
|---|---|---|
| **Vineyard** — land/varieties/field work; **Winery** — batches, vessels and cellar operations; **Decisions** — predictions, alternatives and plans | **Observations** — typed measurement quality; **Weather** — immutable forecasts; **Knowledge** — copyrighted books/web evidence and RAG | **Connectivity** — adapters, source receipts, replay; **AIExecution** — model/provider/agent gateway and cost/egress policy |

One local **.NET 10 modular monolith + PostgreSQL/pgvector + private files**; pay-per-use external AI only when justified. Cross-module communication is through owner-controlled contracts, no direct foreign EF entities or writes. Future sensors, station, new measurements, model vendors, UIs, notifications, cloud and multi-tenancy are **extension stages**, not extra MVP services. Additive schema migrations are acceptable; canonical facts and source provenance must survive.

## Business acceptance and safeguards

- Expert-review **30–50 representative cases** for safe next action, time, multiple acceptable alternatives, citation grounding and Polish applicability; compare against cheaper baselines. **The goal is not yet measured.**
- Test scoped recomputation, as-of forecast/operations replay, budget limits, idempotent restart and the absence of unnecessary LLM calls.
- Private scanned books and derived content remain in protected local storage, outside Git. Source rights and external model egress policy apply before evidence leaves the computer; no public access required for MVP.
- **Parallel implementation:** freeze compiled contracts and migration strategy (Wave 0), then seven independently owned modules (Wave 1), integrate Decisions and workflows (Wave 2–3). The [review](docs/70-quality/architecture-review-2026-11.md) records remaining Wave 0 blockers; no running solution is claimed.

## Documentation

Start with the [business HLD](docs/10-architecture/business-hld.md), [architecture guardrails](docs/00-governance/architecture-guardrails.md), [bounded context map](docs/20-domains/context-map.md), [component design](docs/30-components/overview.md), [LLD index](docs/90-lld/README.md), [contract freeze](docs/90-lld/implementation-contract-freeze.md), [evaluation protocol](docs/90-lld/evaluation-protocol.md), [delivery plan](docs/80-delivery/implementation-dag.md) and [agent collaboration rules](AGENTS.md).


## Monorepo for implementation

The repository now contains a .NET 10 [solution scaffold](wAInery.slnx) and a [local-first monorepo guide](docs/80-delivery/monorepo-bootstrap.md): 8 module contract assemblies, 8 isolated implementation projects, BuildingBlocks, one localhost Host, an architecture dependency check and local PostgreSQL/pgvector Compose. It is **not yet a running decision system**, nor have actual database migrations or agronomic evaluations been executed.

```bash
dotnet restore wAInery.slnx
dotnet build wAInery.slnx
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
```

See [Wave 0 blockers](docs/70-quality/architecture-review-2026-11.md) before assigning parallel module implementation.

## How to implement with coding agents

For the MVP use **VS Code + Codex in scoped Git worktrees**, not an uncontrolled swarm. Four implementation workstreams share one Integration Owner and independent QA; start with one or two coding sessions, only expand after Wave 0 contracts and Postgres migrations work. Each PR must be tested, independently reviewed, cost-aware and traceable to a business outcome.

- [Mandatory agent instructions](AGENTS.md)
- [Agent team and domain ownership](docs/00-governance/agent-team-topology.md)
- [SDLC, branch and review policy](docs/80-delivery/agentic-sdlc.md)
- [Testing and evaluation gates](docs/70-quality/implementation-test-strategy.md)
- [Codex IDE and Git worktrees](docs/80-delivery/codex-development-workflow.md)
- [Agent assignment template](agents/templates/implementation-task.md)

**Current CI:** solution build and module reference graph. Compiling public business ports, real PostgreSQL migrations, integration tests and 30–50 expert scenarios are **next implementation work**, not existing functionality.

## Git workflow for implementation teams

**Trunk-based development:** `main` is the only permanent integration branch. One small, independently testable feature per short-lived branch/worktree; **fetch + rebase onto current `main`** before final tests/review; **squash merge** to keep history clean; delete merged branch and worktree. Do not begin long-lived per-module or `develop` branches. See [mandatory agent collaboration rules](docs/00-governance/trunk-based-collaboration.md), [SDLC](docs/80-delivery/agentic-sdlc.md) and [PR checklist](.github/pull_request_template.md).
