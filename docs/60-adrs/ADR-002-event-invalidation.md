# ADR-002 — Durable scoped invalidation with cheap recomputation

**Status:** Proposed, Gate 3.

Decision: canonical owner writes fact + outbox in one PostgreSQL transaction. Dispatch is at-least-once, consumer dedupes and atomically marks only affected prediction scopes stale and creates leased jobs. Coalesce repeated normal changes; immediate deterministic critical checks; only material decision changes use paid LLM. `inputRevision` prevents publishing outdated plans.

Alternative rejected for MVP: one agent call per update, fan-out to all specialists, Kafka/stream-processor, and fire-and-forget in-process event only. Consequences: more application code/contract tests; much lower model spend and durable restart recovery.
