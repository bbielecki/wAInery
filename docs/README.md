# Architecture documentation

- `00-governance/` review gates, parallel agent coordination and ownership.
- `10-architecture/` repository structure / high-level architecture.
- `20-domains/` **Gate 2 proposed bounded contexts** (awaiting acceptance).
- `30-components/` per-module LLD, **only after Gate 2**.
- `40-contracts/` prose contract reference, **only after Gate 3**.
- `50-data/` canonical and normalized persistence design.
- `60-adrs/` decisions.
- `70-quality/` self reviews, evaluations, security checks.
- `80-delivery/` agent implementation packages.

## Gate 3 design (proposed)

- [Component map](30-components/overview.md)
- [Business traceability](10-architecture/business-requirements-traceability.md)
- [Public contract catalog](40-contracts/catalog.md)
- [Persistence strategy](50-data/persistence-plan.md)
- [Parallel implementation](80-delivery/implementation-dag.md)
- [Review findings](70-quality/gate3-component-review.md)
