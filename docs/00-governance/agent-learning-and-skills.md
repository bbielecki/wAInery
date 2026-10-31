# Agent learning, shared memory and reusable skills — engineering policy

**Goal:** make each implementation increment leave the *next team* better informed, without allowing agents to autonomously rewrite architecture or accumulate unreliable instructions. This is a **development workflow** only; it does not change wAInery's runtime `Knowledge` bounded context, commercial RAG requirements or local-first privacy model.

## Knowledge hierarchy — one truth per topic

1. **Business goal / safety / approved architecture:** `README.md`, business HLD, architectural guardrails, ADRs — human/architect approved.
2. **Executable contract and behavior:** real `Contracts`, migrations, tests, source code — module/integration owned.
3. **Verified engineering experience:** `agents/knowledge/lessons.md` and `patterns.md`, each with test/PR evidence and limits.
4. **Repeatable workflows:** repo-local `.agents/skills/<skill>/SKILL.md`, reviewed and versioned with Git.
5. **Task-level progress:** GitHub Issue/PR + handoffs — not knowledge-base memory.

**Priority:** Never let a skill, lesson, or prior agent output override tests, approved HLD/ADR or a current public contract. Conflicts are reported to the architect; only the proper owner changes canonical documentation.

## Continuous learning: after every substantial implementation PR

- **Implementer:** record one reusable finding **if material**: symptom, cause, actual proof, fix, scope, downside, and link to PR/test; otherwise write `No transferable finding` in handoff. Do not produce artificial knowledge entries.
- **Consuming agent(s):** explicitly assess whether the change affects their interface, error model, read semantics or tests. Acknowledge or request a contract PR in `agents/handoffs/`; no editing another module.
- **Independent reviewer:** challenge incorrect generalizations; check one negative/replay/privacy case before accepting a shared pattern.
- **Architect/Integration Owner:** at each milestone review, inspect recent lessons and repeated review comments, integration mistakes, test commands and handoffs. Decide which repeats merit skill extraction or script automation.
- **Product Owner:** approves learning that would change business constraints, chemical/physical safety, private data egress, budgets or scope. Progress beyond a review gate still requires explicit authorization.

## Architect's skill-extraction trigger

After **two or more genuinely repeated** tasks, failures or review corrections, ask:
1. Is the workflow stable, discrete and independently testable (not a broad agent persona)?
2. Will reuse reduce wrong edits, token cost, reviewer time or omissions?
3. Is an existing skill/script/AGENTS.md rule already sufficient?
4. Is the scope explicit: *when to use*, *when not to use*, inputs, outputs and stop conditions?
5. Can it be tested on at least one **positive and one negative/edge** synthetic scenario without paid APIs?
6. Will it remain useful after likely contract/API changes, or create more maintenance than value?

**If yes:** propose a **small separate skill PR**; don't silently add an unreviewed skill in every feature branch. Use `$skill-creator` if available; otherwise create a standards-compliant `SKILL.md` (YAML `name`/`description`) and only necessary supporting files. Validate routing triggers and resulting output on a representative task before approval. Record owner, evidence and verification/review reference in [skill registry](../../agents/knowledge/skill-registry.md).

**If no:** keep it as a linked lesson, compact pattern or existing CI check. No threshold obliges creating a skill; avoid prompt bloat and redundant skills. When a script can objectively enforce policy (e.g. build/arch check), prefer deterministic CI over asking an LLM to remember.

## Cross-team support without shared write conflicts

- Knowledge contributions use **short-lived branches and PRs**; the architect or designated knowledge curator is **single-writer for shared `agents/knowledge/**` and `.agents/skills/**`**. Module agents propose updates in scoped handoffs or a separate reviewable PR assigned to the curator.
- A module needing help from another module sends a compact request: ticket, expected typed port, payload/fixture, failing test, expected behavior, deadline/dependency, and owner. The receiving agent responds with a contract/example or a scoped review, **not direct foreign-table access**.
- Relevant lesson/skill links are added to next work orders; agents read only task-relevant context, not every past conversation.
- At each milestone gate the architect provides a **Learning Digest** in the gate report: improvements found, patterns reused, candidate skills, retired skills, and measurable impact when possible. Avoid tracking speculative per-agent intelligence scores.
- No agent may update an approved decision boundary, protected-data policy or release gate by editing a lesson or skill; use ADR + Product Owner/Architect review.

## Security and cost guardrails

Knowledge files, skill scripts and tests must not contain scans, OCR text, embeddings, real private user records, external LLM contexts, API secrets, tokens, or licensed passages. Use synthetic examples. Skills are instructions and potential executable scripts: an independent reviewer inspects all shell/network/git effects. No unbounded loops, unattended paid provider calls, auto-merge, deletion of non-merged work, or direct cloud upload of private book contents.

## Git/quality and completion

Each skill/knowledge PR follows [trunk rules](trunk-based-collaboration.md): rebase to latest `origin/main`, CI, independent review, squash merge, delete branch. Skill changes include evidence of tested invocation/negative cases or explicitly identify verification still needed. **A reusable instruction is not a substitute for running the test.**

[Knowledge base](../../agents/knowledge/README.md) | [Skill registry](../../agents/knowledge/skill-registry.md) | [Team topology](agent-team-topology.md) | [SDLC](../80-delivery/agentic-sdlc.md).
