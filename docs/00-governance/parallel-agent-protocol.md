# Parallel agent engineering protocol

## Work isolation

- An implementation unit is an assigned module with an exclusive file ownership boundary.
- Each agent receives an assignment file declaring scope, owned paths, upstream dependencies, contract version, acceptance tests and no-touch paths.
- Other modules are integrated exclusively through `*.Contracts` assemblies and/or machine-readable `contracts/` schemas.
- Shared changes are proposed in `agents/handoffs/` and accepted by an integration owner before affected agents implement them.
- A module's persistence schema and internal types are private; only owning agent may change them.
- Inter-module communication is synchronous through contract interfaces or asynchronous through published, versioned event schemas.
- Cross-context calls that need atomic consistency require documented orchestration; no direct writes to another context's tables.

## Required independent deliverables

1. LLD of owned module, domain model and data ownership.
2. Interfaces, event schemas and examples; backwards-compatibility analysis.
3. Migration and data-replay plan where needed.
4. Unit, contract and integration tests, including idempotency and failure paths.
5. Handoff describing dependency changes, trade-offs, unresolved questions and cost implications.

## Shared integration lock

`src/Host`, `src/BuildingBlocks`, `contracts/`, database bootstrap, CI, dependency matrices, and root solution files are **integration-owned**. Agents do not edit these independently.

## Quality gates

- Explicit contract tests for producers and consumers; no implicit direct persistence coupling.
- No dependency cycle between modules.
- No source text or secrets in model telemetry or committed fixtures.
- Every recommendation includes evidence references, input snapshot IDs, uncertainty and model/policy version.
- All automated retries are bounded; safe degradation when AI services are unavailable.
