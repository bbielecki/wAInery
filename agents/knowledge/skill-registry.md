# Skill inventory and ownership

Skill files are **reusable coding-agent instructions**, not product features and not background workers. Codex repository-local skills are stored at `.agents/skills/<skill-name>/SKILL.md`. Codex reads skill metadata and loads the body when selected; `AGENTS.md` remains the always-applicable policy.

| Skill | Trigger | Owner | Validation / exit |
|---|---|---|---|
| [wainery-pr-verification](../../.agents/skills/wainery-pr-verification/SKILL.md) | Before final code PR submission or merge | Integration Owner / QA | Actual build/check commands, scoped extra tests, no false claims |
| [wainery-contract-change](../../.agents/skills/wainery-contract-change/SKILL.md) | When a module needs a public DTO, event or cross-context interface change | Integration Owner | Proposal → owner decision → versioned compatibility tests |
| [wainery-learnings-review](../../.agents/skills/wainery-learnings-review/SKILL.md) | At PR handoff or milestone review, when identifying reusable learnings | Architect / independent reviewer | Source-backed lesson, repetition screening, optional skill proposal |

## Lifecycle

**Candidate → Scoped draft → Tested → Architect-approved → Published → Observed → Revised or Retired.** Skill metadata must have `name` and concise `description` defining trigger. It must explicitly say when not to use it, inputs, deterministic steps, expected outputs and stop conditions. Add scripts only when repeated manual commands are error-prone and a test proves the script reliable.

Current three starter skills **codify already agreed repository workflows**; they are not evidence that the implementation teams have already learned from completed code changes.

Do not create a new skill just because two tasks share a filename. Architect checks the repository and existing skills for duplication, coherence with HLD/ADR, measurable benefit and need for tests. Re-evaluate a skill when the linked contracts, tooling or release process changes.

[Policy](../../docs/00-governance/agent-learning-and-skills.md) | [Shared knowledge](README.md).
