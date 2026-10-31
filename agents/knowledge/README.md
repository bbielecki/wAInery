# Shared engineering knowledge — wAInery

This is the **durable, version-controlled engineering memory** shared by Codex implementation agents. It is **not** the wine knowledge RAG in `Knowledge`, not private user data, and not an autonomous training database. Every entry is an inspectable Git change.

## Where knowledge lives

| Kind | Canonical source | Editing rule |
|---|---|---|
| Binding architecture, invariants, ownership | `docs/00-governance/`, `docs/20-domains/`, `docs/60-adrs/` | Architect + relevant owner approve; ADR for changed decisions |
| Concrete C# / API contracts, schema | Module `Contracts`, `contracts/`, schema/migrations | Integration Owner gate; tests required |
| Verified engineering lessons and pitfalls | [Lessons](lessons.md) | Link a reproducer/PR and accepted correction; reviewed before merge |
| Successful reusable technical patterns | [Patterns](patterns.md) | Scope, applicability, trade-offs, tests, source PR |
| Executable repeatable agent workflows | `.agents/skills/<name>/SKILL.md` | [Skill lifecycle](../../docs/00-governance/agent-learning-and-skills.md) |
| In-progress issue/PR progress | GitHub Issues and PRs | One canonical progress source; avoid duplicating status in memory pages |
| Agent handoff/proposal | `agents/handoffs/` | Task owner writes; receiver acknowledges in PR |
| Vineyard books, OCR, field notes | Private local storage (ignored) | **Never store here or in skills/fixtures** |

## What to record

**At the end of a meaningful PR**, identify:
1. What was learned and what evidence proves it (test, reproducer, review finding or accepted PR).
2. Which module/consumer might reuse it.
3. Whether it is a durable pattern, one-off troubleshooting note, binding ADR or candidate skill.
4. What remains uncertain — do not promote a guess into canonical guidance.

Use concise, searchable headings and links. Prefer one verified reusable finding over a narrative session diary. Do not repeat documentation already in LLD; **link to the owner document**. An agent should read the task-specific section, not blindly load the entire knowledge base into a prompt.

## Agent-to-agent learning loop

**Implementer** writes a lesson or handoff → **receiving module owner** checks impact against its contracts → **Independent QA** verifies evidence and negative case → **Architect/Integration Owner** decides if the lesson becomes a shared pattern or skill → future agents reuse it via a scoped assignment and report failures. If a pattern changes business semantics, a human Product Owner approves an ADR first.

**Reuse must be earned**: no speculative skills for one-off tasks; after at least two substantive repetitions, evaluate whether a small tested skill lowers errors/rework or token spend. Manual procedures that are already trivial do not need a skill.

[Agent learning policy](../../docs/00-governance/agent-learning-and-skills.md) | [Skill registry](skill-registry.md) | [Agentic SDLC](../../docs/80-delivery/agentic-sdlc.md).
