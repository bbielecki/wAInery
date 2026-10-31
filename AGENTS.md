# wAInery — instructions for coding agents (Codex / other coding assistants)

**Read this before editing any file.** You are contributing to a **.NET 10 modular monolith** in a monorepo, not implementing the runtime winery AI agent platform. The repo's business contract is [Business HLD](docs/10-architecture/business-hld.md); delivery rules are [Agentic SDLC](docs/80-delivery/agentic-sdlc.md). The [team topology](docs/00-governance/agent-team-topology.md), [test strategy](docs/70-quality/implementation-test-strategy.md), [Codex guide](docs/80-delivery/codex-development-workflow.md) and [engineering learning policy](docs/00-governance/agent-learning-and-skills.md) are mandatory for implementation tasks.

## Mission — do not lose the business goal

For a Polish vineyard/block or wine batch, deliver **the safest useful next action, timing, alternatives and cited rationale**, updated incrementally after accepted data changes. Validate decision quality on 30–50 expert-reviewable scenarios, measure cost per accepted plan and avoid unsupported probabilities. MVP: **1–2 private books, curated web sources, confirmed descriptive operations, free weather forecast**. No IoT/cloud/public access/notifications/multi-agent swarm required now.

## Before coding

1. Read your **approved issue/work order** in [template](agents/templates/implementation-task.md), module design in `docs/30-components/<Module>.md`, public interfaces in `src/Modules/<Module>/Contracts/` and applicable `docs/90-lld/` sections.
2. **Wave 0 gate:** compiled public ports/DTOs and actual PostgreSQL migration tests must be established by Integration Owner before concurrent persistence work. Current repository scaffold and prior design sketches are **not** equivalent to completed Wave 0.
3. Confirm your assigned module, path allowlist, `BR-` requirement, fixture IDs, test plan, budget and upstream dependencies. If no approved bounded task exists, propose one; do not implement an unspecified entire context.

## Protected ownership and dependency rules

- **Integration Owner only:** `wAInery.slnx`, `src/BuildingBlocks/**`, `src/Host/**`, `contracts/**`, `infra/**`, `.github/**`, shared migration assembly, root configs and shared fixtures.
- **Module agent only:** its own `src/Modules/<Module>/**`, `tests/Modules/<Module>/**` and scoped handoff. Contract changes are proposed first; the integrator alone changes shared/consumer-facing contracts.
- A module can import another module's **Contracts**, never another module's implementation or EF entities/tables. `Decisions` owns predictions, plans and ranking; `AIExecution` only provides controlled model execution. `Vineyard`/`Winery` own confirmed work, not `Connectivity`/LLM.
- Use `agent/<module>/<issue-short-name>` feature branches, isolated **git worktrees** for simultaneous Codex sessions, one PR per bounded behavior **directly into `main`**. **Mandatory: fetch and rebase onto latest `origin/main` before final CI/review/merge; repeat rebase if trunk moved.** Default **squash merge** for a clean single-commit trunk history; no long-lived integration/develop branches. Delete merged feature branches and local worktrees after successful integration. Never force-push `main`, rewrite another owner's files or silently resolve cross-context ambiguities. Shared changes require a separate Integration Owner PR / ADR.

## Domain, data and safety invariants

- Accepted canonical write + durable outbox must be atomic; retries and processing must be idempotent. Every accepted change invalidates **only dependent** prediction scopes, with no unnecessary paid LLM call and protection against stale run overwrites.
- Keep batch identity separate from vessel occupancy, source observation/arrival time from decision **as-of**, and unconfirmed notes from performed operations. LLM outputs are **untrusted proposals**; actual operations require owner confirmation.
- Cite data/evidence and preserve source rights, version, locality, uncertainties and model provenance. **No fabricated numerical probabilities**; unavailable means unknown, not zero. External LLM egress is **DENY by default** for protected texts unless explicitly authorized.
- Never commit or upload scanned copyrighted books, OCR text, private vineyard/winery history, real embeddings, prompts containing protected excerpts, secrets or tokens. Use synthetic sanitized fixtures. No uncontrolled external tools, unbounded retries or automatic physical/chemical interventions.
- Keep MVP cheap: one local .NET host, PostgreSQL/pgvector, local files, local deterministic evaluation, limited API model calls; do not introduce broker/K8s/Redis/SaaS/paid crawler without a measured justification and ADR.

## Required implementation workflow

**Issue → short design → frozen ports/fixtures → failing deterministic test → minimal implementation → local tests → rebase on current main → independent PR review + updated CI → squash merge → delete branch/worktree → regression check.** See [SDLC](docs/80-delivery/agentic-sdlc.md) and [mandatory trunk policy](docs/00-governance/trunk-based-collaboration.md).

Baseline commands:

```sh
dotnet restore wAInery.slnx
dotnet build wAInery.slnx --configuration Release
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
```

`dotnet test wAInery.slnx`, Testcontainers migration tests, RAG/AI evaluation and contract suites **must be added in later waves**. Do not claim they pass until executed. Ordinary CI must not require paid model calls, live weather endpoints or real private documents.

## Definition of agent handoff

For every PR report: ticket/BR, affected modules and contract versions, scope and non-goals, file list, **exact tests actually executed** and failures/not-run reasons, data migration/outbox risks, rights/security checks, API costs, independent review findings and any blocked dependent work. No self-approval of safety-critical code, no merge on failed checks; no merge when behind main, even if diff is conflict-free. Do not claim work continues in the background when the agent session stops.

**STOP and ask Integration Owner** before touching cross-module contracts, schema ownership, privacy/rights policy, economic limits or core domain boundaries. For business/safety decisions that cannot be inferred from accepted HLD, escalate to Product Owner.

## Shared engineering knowledge and skills (mandatory habit)

- First check [shared engineering knowledge](agents/knowledge/README.md), [verified lessons](agents/knowledge/lessons.md), [patterns](agents/knowledge/patterns.md) and [skill inventory](agents/knowledge/skill-registry.md) **only for content relevant to this task**. Do not read every note into context or treat past agent prose as authoritative. Approved HLD/ADR, tests and public Contracts win on conflict.
- Reuse applicable **repo-local Codex skills** in `.agents/skills/<name>/SKILL.md` (for example PR verification, contract-change coordination and learning review). Skills are versioned instructions, **not** autonomous background workers and not the product's Knowledge/RAG store.
- After each substantive PR, record a reproducible, evidence-backed transferable lesson or explicitly report `No transferable finding`. Link the test, affected module and PR. Share interface/contract findings with other module owners through `agents/handoffs/`; receiving owner reviews and acknowledges the impact.
- **Architect / Integration Owner** audits repeated commands, review mistakes and handoffs at each milestone. When a narrow workflow repeats (normally at least twice) and the benefit exceeds maintenance, propose a tested skill using `$skill-creator` where available. Use a separate small PR, documented owner, trigger/non-trigger examples and independent review. Prefer deterministic CI/scripts for enforceable checks; never manufacture skills or knowledge entries just to satisfy a quota.
- Shared `agents/knowledge/**` and `.agents/skills/**` are curated by Architect/Integration Owner; feature agents submit suggestions or a separate scoped PR rather than colliding on shared files. No private books, OCR, embeddings, notes, raw prompts, credentials, unverified anecdotes or fabricated success metrics in Git.
- At each milestone Gate, report a short **Learning Digest** (accepted lessons, skills reused/proposed/retired, teams informed and measured benefit if available) and **stop for human review** before the next Gate. A skill cannot bypass GitHub tests or required Product Owner approval.
