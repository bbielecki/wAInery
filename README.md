# wAInery — Smart Winery AI

> **Status:** Gate 1 repository layout **accepted**; Gate 2 bounded contexts **accepted**; Gate 3 component architecture **proposed for approval**. No implementation or GitHub publication yet.

A **local-first**, cost-constrained, modular decision-intelligence platform for a Polish vineyard and winery. The MVP deliberately uses 1–2 private books, curated internet sources, descriptive activity history and free weather forecasts. The primary measure is **evidence-backed, feasible next actions and timing** under real Polish conditions, compared against a reference evaluation set — not UI richness or agent count.

## Non-negotiable

1. Cheap local .NET + one PostgreSQL/pgvector + local file storage; optional metered AI API, no always-on cloud/broker.
2. Canonical data ownership across eight approved contexts; reusable contracted ports, inexpensive additive schema changes allowed.
3. After each accepted data change, identify dependent prediction scopes; run AI only when warranted by decision impact.
4. Compare feasible actions, admit uncertainty, cite evidence, never fabricate numeric probabilities.
5. Protected privately scanned books remain local under source rights policy; no source text, OCR, embeddings or private records in Git.
6. Agents are implementation roles and runtime logical specialists, not necessarily separate deployed services.
7. Extensible IoT sources, AI models, UIs, notification channels and future paid hosting without rewriting canonical data.

Read [architecture guardrails](docs/00-governance/architecture-guardrails.md), [component design](docs/30-components/overview.md), [domain context map](docs/20-domains/context-map.md), [review gates](docs/00-governance/review-gates.md), and [agent protocol](docs/00-governance/parallel-agent-protocol.md).
