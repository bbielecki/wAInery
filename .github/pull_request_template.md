## Business result and scope

- **Issue / BR ID:**
- **Bounded context / stream owner:**
- **One user-observable outcome and acceptance scenario:**
- **Non-goals:**
- **Allowed file paths / shared changes approved by Integrator:**

## Contracts and ownership

- **Published/consumed port and event versions:**
- **Producer + consumer compatibility:**
- **Domain data owner and migration proposal (if relevant):**
- **No forbidden module implementation references:** Yes / No / N/A

## Testing — record commands *actually run*

- [ ] Targeted unit tests and negative case
- [ ] `dotnet restore wAInery.slnx`
- [ ] `dotnet build wAInery.slnx --configuration Release`
- [ ] `dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj`
- [ ] Contract JSON examples / compatibility (if touched)
- [ ] Actual PostgreSQL + pgvector Testcontainers migration/outbox tests (if touched)
- [ ] Incremental replay, stale revision and zero unnecessary AI calls (if touched)
- [ ] Source rights / default-deny egress / no protected text in logs (if touched)
- [ ] Evaluation fixtures with independent domain reviewer (when relevant)

**Results/CI links:**  
**Not executed and why:**  

## Cost, risk and operational behavior

- **Measured or estimated model/API cost:** 
- **Fallback when provider/LLM unavailable:** 
- **Idempotency/retry/stale state behavior:** 
- **Private data, books, secrets and unauthorized egress:** Confirm not committed/transmitted.
- **Known limitations and follow-up work:**

## Review and merge decision

- [ ] Implementer self-review completed
- [ ] Independent reviewer checked diff and executable evidence
- [ ] Integration Owner approved shared contract/schema edits, if any
- [ ] CI checks are green (not merely described as green)
- [ ] Human Product Owner consulted when changing domain safety/budget/acceptance expectations

Do **not** merge on unresolved P0/P1 findings, cross-module contract incompatibility or failed required checks. See [agent-assisted SDLC](../docs/80-delivery/agentic-sdlc.md).
