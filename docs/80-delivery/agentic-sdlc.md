# Agent-assisted SDLC — requirements to reviewed merge

This is the **delivery procedure**, not an AI framework to add to the product. Applicable to every human or Codex implementation task; root [AGENTS.md](../../AGENTS.md) summarizes mandatory instructions. [Team roles](../00-governance/agent-team-topology.md) | [Test plan](../70-quality/implementation-test-strategy.md) | [Business HLD](../10-architecture/business-hld.md).

## Delivery model

Use **trunk-based development with short, task-scoped feature branches and GitHub PRs into `main`**. [Mandatory trunk policy](../00-governance/trunk-based-collaboration.md) requires explicit rebase on current `origin/main`, updated CI, squash merge and branch deletion. Protect `main` with required CI and review when repository settings permit. An implementation agent never force-pushes `main` or self-merges a failed check; credentials, source texts or real vineyard history never go to Git. No branch may assume the local machine is always running.

A branch addresses one vertical user-observable change or one **well-bounded prerequisite**; avoid a PR implementing an entire bounded context before checking it in isolation. Approved interfaces and event schemas are contracts, not invitations to redesign the domain.

## Lifecycle, artifacts, gates

| Step | Responsible | Output | Exit / reject gate |
|---|---|---|---|
| **0. Intake** | PO + Architect | Issue with `BR-` traceability, subject, context, measurable result, budget, owner and dependencies | Scope clearly in MVP; no speculative platform extras |
| **1. Plan** | Implementer | Small execution plan, touched paths, contract versions, failure and cost considerations | Integration Owner confirms seam; separate ADR for boundary changes |
| **2. Contract-first** | Integration Owner for shared surfaces, module owner for private details | Compiling C# port/DTO, wire JSON Schema/examples and producer/consumer tests | Contract version agreed; schema compatible; ownership correct |
| **3. Test-first** | Implementer + QA | Failing deterministic test for behavior and negative case; mocks for AI/remote weather | Test can fail for intended reason; no dependence on paid API |
| **4. Implement** | Module implementer | Minimal code within allowed paths, domain outbox and metrics where required | No duplicate business state, cross-module EF reference or unnecessary services |
| **5. Self-check** | Implementer | Focused unit and integration results, architecture scan, exact commands and costs | Local pass or documented environment limitation; no misleading claims |
| **6. Independent review** | Separate reviewer + Architect as needed | PR review comment, high/medium/low findings, ADR when needed | No unresolved correctness, contract, confidentiality, safety or cost-blocking findings |
| **7. Merge** | Integration Owner or authorized human | Squash/merge PR after required checks | Updated branch, checks green, evidence and handoff in PR |
| **8. Verify** | QA + Product Owner | First vertical flow, scenario evaluations, cost/perf baseline and rollback note | User-facing quality gate met or explicitly flagged not met |

**Do not skip from planned design to seven simultaneous implementations.** Wave 0 (compiled contracts + real SQL migration test + one vertical slice) is mandatory and **not currently complete**. The committed monorepo currently validates structure/build only.

## Issue-to-PR routine

1. Select one outcome from [MVP vertical slice](mvp-vertical-slice.md) or [business traceability](../10-architecture/business-requirements-traceability.md). Assign an owner/stream, acceptance fixture IDs and affected public contracts.
2. Create `agent/<module>/<issue-short-name>` from fresh `main`; if concurrently running local Codex agents, create a **separate git worktree per branch**. Codex cloud tasks use isolated workspaces and must produce reviewable PRs.
3. Feed the implementer **only relevant** docs: root `AGENTS.md`, module component LLD, current public Contracts, ticket, examples and business test. Avoid the entire repository in the prompt.
4. Write a brief design/test plan in PR or `agents/handoffs/<ticket>.md`. If a public contract must change, stop feature code and submit integration-owned contract change first.
5. Implement in small commits; run targeted tests every change and whole solution/architecture/contract suite before PR.
6. Open PR with file diff, tests and failure paths, contract version, budget impact and exact LLM/online dependencies. Reviewer must independently examine PR, not rely solely on agent explanation.
7. **Always** `git fetch origin && git rebase origin/main` before final review and merge; push rebased task branch with `--force-with-lease` if necessary, never force `main`. If main advances after CI, rebase again and rerun checks; review/test every conflict. Integrator checks schema migration ordering and semantic compatibility before merge.
8. After **squash merge to `main`** check the **whole** canonical workflow; record regression fixture when a bug is found. Delete merged remote branches and remove local worktrees/branches; retain handoff in Git. No permanent `develop` or team integration branch.

## Branch and commit conventions

- Branch: `agent/vineyard/confirm-field-operation`, `agent/weather/immutable-forecast-runs`, `integration/wave0-public-contracts`, `test/evaluation-frost-coverage`.
- Conventional commits, scoped: `feat(weather): persist immutable forecast runs`; `test(decisions): reject stale publish revision`; `fix(knowledge): block unapproved evidence egress`.
- PR size: ideally 1 behavior, 1 owning module; a cross-cutting PR is Integration Owner-led and explicitly lists affected consumers. Never bypass CI through a "documentation-only" waiver that also changes code.
- Version contracts when a public field or interpretation changes. Expand → deploy consumers → contract only when all consumers migrate; no destructive history rewrite.

## Non-negotiable invariants

**Data and domain:** Each accepted canonical update produces a durable event in the **same DB transaction**; retry and replay are idempotent. Source observation time, received time, weather run time and as-of time are distinct. A batch is not a vessel. AI-proposed operations are drafts until owner confirmation.

**Predictions:** Impact analysis computes dependent scopes; irrelevant changes must generate **zero external AI invocations**. Recompute is revision-aware and cannot overwrite newer state; stale status is visible. Side effects from duplicate events or replays must not duplicate action plans.

**Evidence and cost:** Curated web/Polish conditions and confirmed history may outweigh a non-transferable book reference. Citations and rights attach to sourced claims. Model calls go through budget/egress controls; neither LLM confidence nor temperature-like values can be reported as calibrated event probability. Track **cost per accepted plan and benchmark**, not only tokens.

**Safety:** No automatic chemical/physical interventions; validation covers law/label constraints where relevant. Never commit private books, OCR chunks, raw operational records, secrets, or real model contexts; synthetic sanitized fixtures only.

## Required PR / Definition of Done

- [ ] Business requirement and one concrete user-observable behavior identified.
- [ ] Change confined to owned modules; required ADR or integration-owner review for shared files.
- [ ] Public C# contract and JSON schema examples match; consumer/producer tests cover compatibility.
- [ ] Positive + negative business tests, with reliable local fakes for LLM and weather.
- [ ] If persistence changed: migration up/replay/transaction tests against real Postgres/pgvector.
- [ ] If prediction changed: stale revision, no-impact no-call, ordering/duplicate, as-of behavior.
- [ ] If knowledge/model changed: provenance, rights/egress, prompt injection, budgets, citation verification.
- [ ] All existing checks pass, no secrets/licensed content in diff; failures recorded honestly.
- [ ] Reviewer independent; work handoff describes result and follow-on dependencies.
- [ ] PR merged by authorized maintainer; rollback/revert path clear.

## Review priorities (do not invent issues)

P0: domain/safety/security violations, false performed operations, leakage, invalid probabilities, data loss or stale published decisions. P1: incorrect event handling, contract breaking, misleading evidence/uncertainty, runaway cost. P2: maintainability, overly broad patterns or testing weakness with manageable workaround. P3: style. Block merge for P0/P1; discuss P2 pragmatically; avoid churn for P3.

## Metrics and feedback loop

Record `CostPerAcceptedPlan`, unnecessary AI calls, latency from accepted event to updated prediction, percentage of impacted scopes actually recomputed, plan freshness, evidence Recall@5, expert-reviewed AcceptableTopPlanRate and critical forbidden actions. A model-quality improvement must be tested on held-out scenarios; do not optimize scores by modifying fixtures after viewing output.

**CI status at document publication:** `.github/workflows/monorepo-build.yml` runs restore/build/project graph. Actual unit/contract/Postgres/evaluation test projects and required branch protection must be added in Wave 0 and later waves; this document specifies future gates, not existing green checks.
