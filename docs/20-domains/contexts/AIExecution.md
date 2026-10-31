# AIExecution platform boundary

**Class:** Generic/platform. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/AIExecution/`.

## Purpose
Decouple model providers, agent roles, tool invocation, policy-aware data egress and cost instrumentation from the wine/vineyard decision model.

## Owns
`ModelCapability`, `ModelProfile`, `AgentRoleDefinition`, `InvocationRecord` (metadata only), usage/budget policies, tool permissions and provider adapter configs. `ContextBuilder` policy is driven by Decisions and owning data read ports; AIExecution merely enforces call-time limits and returns structured results.

## Invariants
- Domain layer cannot reference vendor SDK classes or provider-specific response types.
- AI output is untrusted *candidate* output; no direct writes to Vineyard/Winery/Decisions canonical aggregates.
- External context includes only authorized spans/fields; private book egress blocked by default unless explicitly enabled. Tool invocation bounded by allowlist, cost, tokens and time.
- Provider switching cannot broaden data access. Missing permitted fallback => fail closed / degrade.
- Trace contains prompts/answers only if an explicit safe redaction/debug policy allows it; default logs only trace ids, model, token usage, costs, latency, status and evidence ids.
- Agent roles are logical; no separate agent server/process necessary for MVP.

## Consumes
Approved scoped requests from Decisions, permitted evidence retrieved from Knowledge, normalized contextual DTOs through explicit query ports.

## Returns
Validated structured findings/action suggestions plus runtime metadata; AIExecution does not publish business `DecisionPlan`.

## MVP acceptance
At least one external model via adapter, ability to swap model config without domain code changes, hard per-run budgets, typed structured response validation and safe unavailable-provider path.
