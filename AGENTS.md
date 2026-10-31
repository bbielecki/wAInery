# Instructions for independent implementation agents

**Project status:** Gates 1–3 accepted by user. Gate 4 LLD drafted and reviewed locally; implementation begins only after integration owner freezes shared contracts and finalizes tests/migration execution. See `docs/90-lld/README.md`.

Read `docs/00-governance/architecture-guardrails.md`, `docs/00-governance/parallel-agent-protocol.md`, your module's `docs/30-components/<module>.md`, and `docs/40-contracts/catalog.md`.

## Non-negotiable

- Optimize for **evidence-based, Polish-contextualized** next-action quality with minimal local running costs; no gratuitous features/infra.
- Work only within allocated module and test folders. Shared files, `contracts/`, Host, BuildingBlocks, migrations bootstrap and CI are integration-owned.
- Use typed public contracts, never another module's EF entities or tables; publish canonical changes through a durable outbox.
- New accepted data changes must produce scoped invalidation; unrelated scopes must not trigger paid AI.
- Domain actions require explicit user confirmation before marking performed; LLM results are non-authoritative suggestions.
- Prohibit invented quantitative probability; preserve provenance, data versions, as-of times and uncertainty.
- Local-only privacy: do not commit PDF/OCR/content, private records, embeddings or credentials; do not leak contents via traces or model contexts without approved rights.
- Use bounded retries, idempotency, budget policy and safe fallback; document all failures and costs.
- Report tests, contract changes, blockers, affected modules and handoffs. No unilateral change of shared contract.
