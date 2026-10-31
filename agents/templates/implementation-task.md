# Implementation agent work order — copy for each issue

> Complete before starting Codex. Integration Owner approves boundary and cross-module dependencies. This is a planning template, not a runnable task.

## Identity and business reason

- **Ticket / branch:** e.g. WX-001 / `agent/weather/forecast-runs`
- **Human owner / architect:** 
- **Codex implementation agent / stream:** 
- **Bounded context owner:** 
- **Business requirement (`BR-xx`) and user outcome:** 
- **Acceptance scenario IDs:** 
- **Priority and MVP scope:** 
- **Stage:** Proposed / Ready / Implementing / Review / Merged / Blocked

## Scope and contracts

- **Allowed file paths:** 
- **Forbidden paths:** Host / BuildingBlocks / cross-module implementations / shared contract schema unless Integration-owned PR.
- **Canonical input port & version:** 
- **Canonical output port / event schema & version:** 
- **Data ownership and transaction boundary:** 
- **Affected migration proposal (integration-owned merge):** 
- **Upstream prerequisites:** 
- **Downstream consumers / handoffs:** 
- **Non-goals:** 
- **Relevant verified lessons/patterns (links, or none):** 
- **Relevant skills to reuse and why (or none):** 

## Context for agent

Read: `AGENTS.md`, `docs/10-architecture/business-hld.md`, `docs/80-delivery/agentic-sdlc.md`, relevant `docs/30-components/<module>.md`, `docs/90-lld/**` section and current `*.Contracts`.

- **Synthetic fixtures provided:** 
- **Relevant evidence/source rights and as-of constraints:** 
- **Failure / replay / idempotency semantics:** 
- **Security / model egress policy:** 
- **Cost limit and acceptable latency:** 

## Acceptance checklist (design tests before implementation)

- [ ] Unit invariants, including one negative case.
- [ ] Producer/consumer contract example + version compatibility.
- [ ] Actual PostgreSQL integration with migration + rollback/replay if persistence touched.
- [ ] Scoped prediction invalidation; zero paid AI for irrelevant updates (if applicable).
- [ ] Rights, citations, no invented probabilities or unsafe actions (if applicable).
- [ ] One user-visible journey / local report if a vertical slice.
- [ ] Traces/logs are free from protected books, user notes and API secrets.
- [ ] Budget/token/cost checks require no live API in ordinary CI.

## Delivery report — fill after work

- **Branch/PR/commit:** 
- **Changes made and paths touched:** 
- **Public contracts changed? If so, integration-owner PR:** 
- **Tests actually run (commands + results):** 
- **Tests not run and why:** 
- **Model/API cost incurred (or zero):** 
- **Evidence of business acceptance:** 
- **Review findings and resolutions:** 
- **Open risks/dependencies:** 
- **Transferable finding + proof (or `No transferable finding`):** 
- **Downstream module owners informed / acknowledgement:** 
- **Repeated workflow or candidate skill proposed for architect review:** 
- **Next recommended task:** 

**Stop condition:** If a frozen public contract is inadequate or ownership is unclear, stop and create a handoff/ADR proposal; do not silently redesign another module.
