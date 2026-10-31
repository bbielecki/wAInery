# Work package — AIExecution (draft, inactive)

**Owner:** implementation agent assigned after Gate 3 approval. **Module:** `AIExecution`.

## Owned paths

- `src/Modules/AIExecution/**`
- `tests/Modules/AIExecution/**`
- Module LLD reviews/errata through `agents/handoffs/` (do not unilaterally edit shared docs/contracts)

## Inputs and contractual seams

- `docs/30-components/AIExecution.md`
- `docs/40-contracts/catalog.md`
- `docs/00-governance/architecture-guardrails.md`
- `contracts/` schemas signed off by integration owner (not yet frozen)

## Deliverables and acceptance

- Module aggregates/DTOs and migrations scoped to own schema; typed ports and canonical events as approved.
- Idempotent write paths, as-of/read semantics, error recovery and module contract tests.
- No direct cross-module schema access; no protected content/secrets in Git.
- Handoff explains outstanding contract needs, migration risks, token/runtime costs and evaluation impact.

## No-touch

`src/Host/**`, `src/BuildingBlocks/**`, `contracts/**`, `infra/**`, other modules and central DB bootstrap. Propose shared changes via `agents/handoffs` to Integration Owner.
