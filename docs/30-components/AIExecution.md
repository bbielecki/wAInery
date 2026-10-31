# AIExecution — component boundary proposal

## Components / ownership
- `ModelGateway`: provider-neutral `IReasoningExecutor` adapter for configured model; retry/fallback only to providers satisfying capability + egress policy.
- `AgentRoleRouter`: select VineyardAdvisor or WinemakingAdvisor from preselected scope; no unsolicited fan-out. DecisionCoordinator is workflow behavior in Decisions (avoid two owners of planning).
- `ToolBroker`: allowlisted, typed, bounded read tools to retrieve evidence/state; no arbitrary SQL, shell, arbitrary HTTP or write to domain aggregates.
- `DataEgressPolicy`: inspect excerpt/source classification before provider call; local-only copyrighted content never sent externally without explicit policy decision.
- `UsageMeter`: input/output token counts, cost estimate, model version, latency, correlation IDs, retry count; hard per-run and monthly budget stops.

## Public contracts
`IReasoningExecutor.ExecuteAsync(AgentAdviceRequest, CancellationToken) -> AgentAdviceResult`; payload includes `workspaceId`, `purpose`, `decisionScope`, `asOf`, `evidenceRefs`, structured facts, `allowedTools`, output schema id/version, token/time budget. Output includes findings/candidates/evidence refs, qualitative uncertainty and usage; forbidden to produce unaudited success percentages.

## Events
No business events emitted. Optional operational `ModelInvocationRecorded` stays internal and contains no raw book passages/prompts by default.

## Data/storage
Schema `ai_execution`: `model_profiles`, `agent_role_versions`, `usage_ledger`, `budget_policies` (no full prompts). Secrets in local protected config / environment outside Git.

## Failure, tests, evolution
No external model -> safe `UNAVAILABLE` with prompt context not leaked to logs, Decisions retains stale/insufficient plan. JSON Schema validation on every response; exact provider mapping in adapter only. Local model and hosted model may coexist later without changing Decisions. Mock adapter enables tests without token costs.
