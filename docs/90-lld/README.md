# Gate 4 — implementation-ready Low-Level Design (LLD)

**Status:** Reviewed design baseline, not implemented. **MVP goal:** best defensible next action and time for Polish vineyard/cellar conditions with minimal fixed and token spend; quality is measured, not assumed.

## Required reading/order
1. [Architecture guardrails](../00-governance/architecture-guardrails.md) and [approved bounded contexts](../20-domains/context-map.md).
2. [Data model](data-and-transactions.md) and [reference PostgreSQL DDL](../50-data/sql/001_foundation.sql).
3. [Ports and contracts](ports-and-compatibility.md), [processing and prediction](incremental-prediction.md).
4. [Decision planning](agents-and-decisions.md), [retrieval](knowledge-and-retrieval.md), [ingestion](source-adapters.md).
5. [Runtime and cost](runtime-and-cost.md), [safety, reliability](safety-and-recovery.md), [evaluation](evaluation-and-acceptance.md).
6. [Parallel implementation](parallel-delivery.md), [review](../70-quality/gate4-review.md).
7. [Prediction catalog](prediction-catalog.md), [Polish-context evidence policy](polish-context-evidence-policy.md), [evaluation protocol](evaluation-protocol.md), [implementation contract freeze](implementation-contract-freeze.md).
8. [MVP vertical slice](../80-delivery/mvp-vertical-slice.md), [extension review](../70-quality/gate5-documentation-review.md).

## Explicit MVP product boundary
- 1–2 **privately stored** scanned books; curated URL imports; owner-written narrative observations and **confirmed** actions; one forecast adapter, without public server or sensors.
- Local app exposes one-person CLI or local HTTP facade; reports saved in database and Markdown. No full UI, outbound notification infrastructure or user auth in MVP. Never expose port publicly.
- Recompute after accepted changes; scope/priority/materiality regulate AI calls. Inactive computer cannot deliver continuous monitoring.
- Domain-owned canonical records; LLM may suggest only; rights policies enforced before provider egress; no invented probabilities.
- **No production code is claimed by this LLD.** SQL is a blueprint, not an executed migration. Tests must be implemented prior to production use.

## Architectural acceptance evidence
- Source receipt → owner-confirmed fact → outbox → scoped invalidation → bounded (possibly no-LLM) recompute → versioned plan → local report.
- Complete reproducible trace of source version, known-at/as-of time, prediction definition, AI model, costs, evidence references and decision alternatives.
- Single local PostgreSQL and process; no RabbitMQ/Kafka/Redis/managed search. Optional Docling process is invoked only for imports.
- All agents implement **their own** module, tests and adapters behind frozen ports; integration owner edits shared contracts/Host.
