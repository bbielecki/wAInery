---
name: wainery-contract-change
description: Coordinate a versioned public C# DTO, port or event change between wAInery bounded contexts before coding consumers.
---

# Propose and validate a cross-context contract change

**Use when:** a module's feature needs an additive/breaking public DTO, method, event, error or semantics change. **Do not use** for private aggregate implementation details or a local refactor with no published contract impact.

1. Read `AGENTS.md`, [domain ownership](../../../docs/20-domains/context-map.md), [contract-freeze rules](../../../docs/90-lld/implementation-contract-freeze.md) and current producer/consumer fixtures.
2. Identify **one owning module** and downstream consumers. Write a scoped proposal in `agents/handoffs/<ticket>-contract-change.md`: requirement/BR, old vs proposed interface/JSON, compatibility type, business semantics, idempotency/errors, workspace/opaque IDs, as-of/units, example fixtures and migration/replay implications.
3. Ask Integration Owner to review. If architecture/data-owner, legal/safety or cost policy changes, request an ADR and appropriate human authorization before any contract edit.
4. Integration Owner creates a **short-lived contract-only branch/PR**, freezes typed C# ports and wire schema compatibility fixtures, runs affected producer/consumer tests and solution reference checks. Consumers implement only after merge/rebase on main.
5. Report impacted agent assignments, new contract version, evidence/CI, planned consumer cutover and outstanding blockers. Link the PR in handoffs and the task.

**Stop:** conflicting ownership, circular implementation references, unverified breaking fields, workspace mismatch or contract change proposed through a feature that modifies foreign module code. **No unilateral shared edits and no silent contract forks.**
