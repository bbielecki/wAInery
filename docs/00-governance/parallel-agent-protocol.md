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

## Short-lived trunk-based collaboration (mandatory)

See [trunk policy](trunk-based-collaboration.md) for full Git commands, branch lifecycle, and enforcement. Every agent works in its own small feature branch/worktree based on current `main`, changes only one bounded behavior, and targets `main` directly. No long-lived `develop`, shared integration branches or per-team trunks. **After implementing and testing: rebase onto fresh `origin/main`, rerun full required checks, obtain independent review, squash-merge; delete remote branch and local worktree.** If `main` advances meanwhile, rebase again. Only Integration Owner changes central contracts/Host/CI/migrations; other agents propose handoffs first.

The GitHub PR policy check verifies that the latest main is an ancestor of the PR head. The cleanup workflow deletes eligible merged PR heads only. The one-time sweep of legacy branches completed successfully and is no longer part of the permanent workflow. The repo admin should make the check required and enforce up-to-date status by branch ruleset.
