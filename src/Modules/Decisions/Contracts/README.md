# Decisions/Contracts

Compiled IPC contracts in AdviceContracts.cs and DecisionReadContracts.cs, IO
freeze G1-04 (#30). Existing JSONv1 schemas remain unchanged; explicit wire mapping
is required, not default serialization of scoped IPC records.

IReasoningExecutor.AdviseAsync belongs here; AIExecution implements it later.
Advice results are untrusted proposals without numeric probability fields. Plan
probability metadata is structural, not evidence of scientific validity. Separate
read freshness cannot extend PlanV1.state or prove real DB catch-up/CAS.

Public module-owned request/response DTOs and query/command interfaces. Cross-module change proposals must be approved by integration owner before freezing.

See `docs/30-components/Decisions.md` and the agent assignment.
