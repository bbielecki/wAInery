---
name: wainery-learnings-review
description: Review verified engineering lessons after a wAInery PR or milestone and identify repeatable work worth a scoped Codex skill.
---

# Extract actual learning, not speculative memory

**Use when:** a substantive PR closes, a recurring review finding appears or a milestone is reviewed. **Do not use** to create entries for trivial PRs, capture private book content, record unverified guesses or modify business/architecture policy without approval.

1. Inspect the diff, relevant tests/CI, PR comments and `agents/handoffs/`. Identify material reproducible learnings (bugs, successful patterns, expensive repeated manual workflows, privacy/rights pitfalls).
2. Check for an existing authoritative ADR, contract, test, `agents/knowledge/` entry, and skill under `.agents/skills/`. Avoid duplication. A chat transcript alone is not evidence.
3. For each material finding, propose a concise entry in [lessons](../../../agents/knowledge/lessons.md) or [patterns](../../../agents/knowledge/patterns.md): symptom, proof, scope, resolution, negative case, trade-offs, consumer modules, PR link and confidence/status. If none, write `No transferable learning` in the PR handoff.
4. If at least **two genuinely repeated** tasks or corrections support a stable, narrow workflow, the Architect compares expected reuse benefits (less review/rework/token spend) with skill maintenance cost and existing automation. A first-use procedure can be documented, but don't churn out skills solely to hit a quota.
5. If justified, draft a **separate skill PR** with `SKILL.md` YAML `name`/`description`, positive/negative triggers, steps, output and stop conditions. Use `$skill-creator` when installed; otherwise author standard format. Add scripts **only if** independently tested and worth the complexity.
6. Independent reviewer validates skill invocation on a positive and negative synthetic case, permission/privacy safety, no redundant instructions, and impact on task outcomes; Integration Owner updates [registry](../../../agents/knowledge/skill-registry.md). Merge only through current main/rebase/CI/review.
7. Include at milestone gate a **Learning Digest**: lessons accepted, skills reused/added/retired, modules informed, measurable benefit if known, and proposed follow-ups. Explicit user approval remains required before the next gate.

**Stop:** a skill tries to bypass approval/CI, requests unattended Git pushes, uses secrets/licensed data, claims unmeasured savings, or contradicts HLD/ADR/contract. Escalate to owner instead.
