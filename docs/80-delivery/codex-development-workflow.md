# Recommended implementation workflow — VS Code + Codex + Git worktrees

**Recommendation for wAInery:** use **VS Code + Codex IDE extension** as the primary environment, local .NET 10 SDK, Docker/Podman Compose and GitHub PRs. Begin with one orchestrating human/architect and **one or two independently scoped Codex coding sessions**, not an unbounded autonomous multi-agent swarm. [Agent team map](../00-governance/agent-team-topology.md) | [SDLC](agentic-sdlc.md).

> The term *coding agent* here means a Codex task/session acting on repository files. It is **not** one of the production AI advisors to be built under the `AIExecution` module.

## Why VS Code rather than custom multi-agent orchestration?

- User remains the design/budget owner and can inspect actual code, SQL and tests in one place.
- The Codex IDE extension can edit, run local commands, compile, and follow recognized repo-level `AGENTS.md` instructions. The same repo also works with Codex CLI.
- The monorepo's project/contract boundaries make tasks safely parallelizable **without** building an additional orchestration service in MVP.
- Git/GitHub PRs provide durable review, rollback, provenance and exact agent handoff. Model memory or chat history are *not* the source of truth.
- Codex Cloud can be used **optionally** for bounded delegated tasks in isolated cloud environments; tasks can continue while your laptop sleeps when an environment is properly configured. Separate tasks do not share unpublished changes.

## Initial local setup

1. Clone `https://github.com/bbielecki/wAInery.git` and open workspace in VS Code.
2. Install .NET 10 SDK, Git, Docker Desktop/Podman with Compose, and official Codex VS Code extension. Authenticate using your approved account. No permanent app server required beyond local Postgres.
3. Run baseline commands and read `AGENTS.md` plus business HLD and relevant module contracts:
   ```sh
   dotnet restore wAInery.slnx
   dotnet build wAInery.slnx
   dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
   ```
4. Prepare local-only DB credentials from `infra/local/.env.example`; NEVER commit `.env`, private books, embeddings, OCR, real operation notes, tokens or prompts holding these data. Enable file access narrowly and approve network/shell commands appropriately.
5. **Wave 0 first**: one integrator task freezes compiling C# ports and Testcontainers-backed migrations. Do not start seven module coders against sketch contracts.

## Run two agents in parallel without file conflicts

Use separate worktrees (examples assume a fresh local checkout of `main`):

```sh
git fetch origin
git worktree add -b agent/weather/forecast-runs ../wainery-weather origin/main
git worktree add -b agent/vineyard/confirmed-operations ../wainery-vineyard origin/main
```

Open **each worktree in a separate VS Code window**. Give Codex in the weather window only the Weather module path and the assigned task; give the vineyard window only Vineyard paths. A third reviewer task may review a PR's diff **without writing to the implementer's worktree**.

Example task prompt:

```text
Read AGENTS.md, docs/10-architecture/business-hld.md,
docs/80-delivery/agentic-sdlc.md, docs/30-components/Weather.md
and the frozen Weather public contracts.
Implement only ticket WX-001 (immutable forecast runs and as-of selection).
Owned paths: src/Modules/Weather/** and tests/Modules/Weather/**.
Do not edit other modules, contracts/, Host, BuildingBlocks or central migrations.
First add deterministic unit/contract tests; no paid LLM or real Weather API
calls in CI. Show a short plan; implement with focused commits; run exact tests;
report unrun checks, cost impact and handoff. If contract changes are needed,
STOP and write a proposal, not unilateral cross-module changes.
```

Replace paths/ticket with the actual approved assignment. Make the test cases and allowed paths explicit; **never** merely ask "implement the entire winery". If a task affects several owners, assign the integration coordinator, not an unrestricted agent.

## Pull-request procedure

- Each task uses a short feature branch based on up-to-date `main`. One behavior/module per PR. Codex may prepare the PR and test report, but a separate reviewer checks the diff, fixtures, contract/data consistency and new model spend.
- CI required: restore/build, project boundaries, and, once implemented, domain unit tests, contract fixtures and actual PostgreSQL integration tests.
- Shared files are Integration Owner single-writer; module agent files are path-scoped. Use small contract PRs before competing module PRs.
- Keep PRs reviewable. Record commands **actually executed** and failures; do not treat generated "tests passed" prose as evidence.
- Prefer integrating one vertical business journey early rather than completing all modules and finding integration gaps late.

## Optional Codex Cloud

Codex Cloud supports independently scoped repository tasks in separate cloud workspaces and can continue without an awake local computer; you can review diffs and results before merging. Configure the cloud environment with a suitable .NET 10 SDK, Postgres/pgvector test setup, approved GitHub access, and **synthetic test data only**. Avoid sending copyrighted scans and real private operational notes into the cloud environment. Isolate tasks by branch/PR and avoid concurrent edits to common contracts.

Cloud task availability and execution limits are account/workspace-dependent. The **ChatGPT conversation with GitHub connector does not itself create unattended Codex cloud jobs**. A chat response cannot promise continued background implementation. Start/manage Codex tasks explicitly through the Codex app/IDE/CLI/cloud environment, or a separately configured harness/automation you control. The same GitHub PR/CI controls apply.

## When to consider a programmable agent harness

**Not in MVP.** If future work demands automatic issue triage, repeated multi-step jobs, resumable runs and PR orchestration, evaluate Codex CLI `exec`, official SDK or Agents API in a separate developer-automation project with strict permissions, quotas and audit logs. That automation is not part of the winery runtime and should be justified by measured productivity, not architectural ambition.

## Practical rollout

| Increment | Live coding sessions | Result |
|---|---:|---|
| **Wave 0** | 1 Integrator (+ independent reviewer on PR) | Compiling contracts, real DB migrations, synthetic fixtures, passing CI |
| **Vertical Slice** | 1 Data Acquisition + 1 Decisions session | Weather → scoped impact → stored local plan, no mandatory LLM |
| **Domain and evidence** | 2–3 isolated streams | Vineyard/Winery operations, observations, RAG/rights, agent gateway |
| **Full MVP** | 1 integrator + module owners as needed; independent QA | Three business journeys, cost metrics, expert-reviewed scenario suite |

## References

- [Codex in VS Code / ChatGPT plan](https://help.openai.com/en/articles/11369540-using-codex-with-your-chatgpt-plan)
- [Codex Cloud tasks](https://help.openai.com/en/articles/20001545-using-codex-cloud)
- [Codex workflow conventions and AGENTS.md](https://developers.openai.com/cookbook/examples/codex/iterating-development-workflows-with-codex)
- [Codex harness / CLI exec / SDK](https://developers.openai.com/blog/codex-as-a-platform)
