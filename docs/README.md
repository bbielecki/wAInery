# Architecture documentation — wAInery

**Business entry point:** [Business HLD](10-architecture/business-hld.md) describes the owner-facing value, MVP boundary, data-to-decision loop, cost restrictions, evidence policy and future phases. Read it before implementing a module.

**Current design status:** Gates 1–3 accepted. Gate 4 LLD reviewed as a design, with explicit Wave 0 blockers; no compilation, migrations or quality results are claimed. See [independent architecture review](70-quality/architecture-review-2026-11.md) and [review gates](00-governance/review-gates.md).

| Directory | Purpose |
|---|---|
| [00-governance](00-governance/review-gates.md) | Accepted boundaries, review gates, agent coordination |
| [10-architecture](10-architecture/business-hld.md) | Product purpose, business traceability, repository layout |
| [20-domains](20-domains/context-map.md) | Eight approved domain/platform boundaries and ownership |
| [30-components](30-components/overview.md) | Approved component map and dependency rules |
| [40-contracts](40-contracts/catalog.md) | Machine schema catalog; C# ports remain design sketches until Wave 0 |
| [50-data](50-data/persistence-plan.md) | Canonical ownership, PostgreSQL reference design |
| [60-adrs](60-adrs/README.md) | Architecture decisions |
| [70-quality](70-quality/architecture-review-2026-11.md) | Reviews, outstanding blockers, evaluation criteria |
| [80-delivery](80-delivery/implementation-dag.md) | Parallel implementation plan and integration waves |
| [90-lld](90-lld/README.md) | Detailed design, prediction, retrieval, agent and runtime specifications |

**Implementation rule:** independent module teams start after the Integration Owner publishes compiling shared contracts and verifies actual database migrations. Read [AGENTS.md](../AGENTS.md) and [contract-freeze guidance](90-lld/implementation-contract-freeze.md).
