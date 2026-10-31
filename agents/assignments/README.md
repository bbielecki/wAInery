# Implementation assignments — issue-driven, not one standing team per module

**Current status:** architecture approved, monorepo scaffold merged, **Wave 0 compiling contracts and live Postgres migrations still pending**. Do not start seven concurrent agents against incomplete public contracts.

Create a bounded assignment per issue by copying [implementation task template](../templates/implementation-task.md), with exactly one owning module, explicit paths/versions, acceptance fixture IDs, testing and handoff. Place finished assignments in this folder as `<ticket>-<module>-<topic>.md`.

**Team topology:** [four implementation streams + Integration Owner](../../docs/00-governance/agent-team-topology.md), with independent QA and human business/domain approvals. Implementation workstreams are not the same as AI runtime advisor roles.

**SDLC:** [steps, PR gates and tests](../../docs/80-delivery/agentic-sdlc.md). **Codex:** [VS Code, worktrees, optional Cloud](../../docs/80-delivery/codex-development-workflow.md). **Testing:** [implementation test strategy](../../docs/70-quality/implementation-test-strategy.md).

Early recommended tickets: Integration Owner Wave 0 contract freeze + PostgreSQL migration proof; then weather → scoped Decisions update → local plan vertical slice. Agents should implement only the task assigned to them, not every module listed in this directory.
