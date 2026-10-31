## Business result and scope

- **Issue / BR ID:**
- **Bounded context / stream owner:**
- **One user-observable outcome and acceptance scenario:**
- **Non-goals:**
- **Allowed file paths / shared changes approved by Integrator:**

## Contracts and ownership

- **Published/consumed port and event versions:**
- **Producer + consumer compatibility:**
- **Domain data owner and migration proposal (if relevant):**
- **No forbidden module implementation references:** Yes / No / N/A

## Testing — record commands *actually run*

- [ ] Targeted unit tests and negative case
- [ ] `dotnet restore wAInery.slnx`
- [ ] `dotnet build wAInery.slnx --configuration Release`
- [ ] `dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj`
- [ ] Contract JSON examples / compatibility (if touched)
- [ ] Actual PostgreSQL + pgvector Testcontainers migration/outbox tests (if touched)
- [ ] Incremental replay, stale revision and zero unnecessary AI calls (if touched)
- [ ] Source rights / default-deny egress / no protected text in logs (if touched)
- [ ] Evaluation fixtures with independent domain reviewer (when relevant)

**Results/CI links:**  
**Not executed and why:**  

## Cost, risk and operational behavior

- **Measured or estimated model/API cost:** 
- **Fallback when provider/LLM unavailable:** 
- **Idempotency/retry/stale state behavior:** 
- **Private data, books, secrets and unauthorized egress:** Confirm not committed/transmitted.
- **Known limitations and follow-up work:**

## Trunk-based branch hygiene (required before merge)

- [ ] This is **one small, independently testable feature/fix** on a short-lived branch targeting `main` (explain exceptions).
- [ ] I ran `git fetch origin && git rebase origin/main` on this task branch **after the last relevant main change**; no unrelated commits or merge commits remain.
- [ ] All required tests/CI reran **after** that rebase and passed at the final head.
- [ ] Feature will be **squash-merged** to `main` (or documented independent-commit exception) and the remote branch/local worktree will be removed after integration.
- [ ] No permanent integration branch, no direct or force push to `main`.

## Review and merge decision

- [ ] Implementer self-review completed
- [ ] Independent reviewer checked diff and executable evidence
- [ ] Integration Owner approved shared contract/schema edits, if any
- [ ] CI checks are green (not merely described as green)
- [ ] Human Product Owner consulted when changing domain safety/budget/acceptance expectations

Do **not** merge on unresolved P0/P1 findings, cross-module contract incompatibility, failed required checks or a branch behind current `main`. See [trunk policy](../docs/00-governance/trunk-based-collaboration.md). See [agent-assisted SDLC](../docs/80-delivery/agentic-sdlc.md).
