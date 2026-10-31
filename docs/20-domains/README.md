# Gate 2 — Bounded contexts (PROPOSAL)

Gate 1 (repository layout) approved. **Gate 2 has not yet been approved.** No implementation code and no component-level contracts have been finalized.

Read in order:
1. [Context map](context-map.md) — core/supporting/platform boundaries and ownership.
2. [Data ownership matrix](ownership-matrix.md) — a single source of truth per concept.
3. [Event and read-model flows](interaction-flows.md) — cross-context communication.
4. [Ubiquitous language](ubiquitous-language.md) — unambiguous terminology.
5. [MVP domain journeys](mvp-journeys.md) — three vertical slices and incremental recomputation.
6. Individual context specifications in `contexts/`.
7. [Review and unresolved decisions](../70-quality/gate2-domain-review.md).

Logical contexts (not microservices): Vineyard, Winery, Observations, Weather, Knowledge, Decisions, Connectivity, AIExecution.

Evaluation is an offline acceptance/test capability in the MVP, not a runtime bounded context. Notifications are deferred and delivered later through a channel abstraction; DecisionPlan output exists from MVP. Security/IP is enforced across contexts and owned by explicit data/policy definitions, not a generic business aggregate.
