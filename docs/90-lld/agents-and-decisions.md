# LLD — Specialized advisors, decision ranking and uncertainty

## Responsibilities
`Decisions` owns decision facts, prediction state, ContextAssembler, hard constraints, candidate merging, ranking and plan revisions. `AIExecution` owns provider calls, chosen roles/models, tool access/egress policy and cost. No LLM directly writes a physical operation or prediction probability.

MVP roles: `VineyardAdvisor`, `WinemakingAdvisor`, `DecisionCoordinator`. These are role definitions and tool allowlists in one process, **not** separate services. `Knowledge Researcher`/`Reviewer` are optional future specialized roles triggered only by uncertainty/complexity, not an unconditional multi-agent chain.

## Advisor invocation contract
`AgentAdviceRequestV1`: `requestId`, `workspaceId`, `subject`, `asOf`, `intent`, bounded state facts, feature aggregates, candidate problem statements, permitted evidence refs, allowed tools, currency/locale and hard token budget. Pre-model egress filters source rights. `AgentAdviceResultV1`: findings, citations, action candidates, confidence `UNKNOWN|LOW|MEDIUM|HIGH`, usage. No numeric `predictedSuccessProbability` from model; the machine schema explicitly forbids extra fields.

## Context construction
- Read owner facades for subject snapshot, as-of operations, linked observations and most recent forecast **known at as-of**; project maximum N changes and aggregated trends.
- Map question to contextual tags: `region=PL`, local location/weather conditions, cultivar, phenology, wine style/process, available equipment. `region=PL` is a preference/relevance feature, **not** an unsupported superiority claim for any source.
- Retrieve top-N lexical + vector segments, fuse, optionally rerank; verify document version, page, policy and citation IDs. Never include entire book; model receives only approved, relevant data.
- Use schema-constrained structured outputs and time/quantity parsing; quotes remain evidence, not system instructions; carry `reasoning_trace` as concise auditable reasoning summary, not chain-of-thought.

## Candidate evaluation
Normalize by `(subject, actionType, target, time-window)`. Candidate fields: `actionId`, description, urgency, `earliestAt`, `idealWindow`, `deadline`, preconditions, mutually-exclusive group, prerequisite IDs, estimated physical cost/time, feasibility, evidence, harm flags, alternatives, approval-required. Always consider `MEASURE`, `OBSERVE`, `NO_ACTION`, `ESCALATE` where applicable.

1. **Hard veto**: legal/product label constraints, physical infeasibility, unknown essential measurement, outdated data, safety and IP policy.
2. **Dependency graph**: check cycles, missing prerequisites, incompatible operations/time windows; reject invalid combinations.
3. **Risk decision**: event probability `p` only if supplied by *named calibrated/expert/validated model* with method, version, basis, evaluation ref and calibration status. `p=NULL` means unknown, never 0. LLM preference is not evidence for p.
4. **Compare utility**: use explicit and configurable weights for expected benefit, irreversible harm, labour/time, resources, knowledge gain (value of information), deadline and uncertainty. In MVP when probabilities/costs are insufficient, use defensible ordinal multi-criteria scores with reasons, NOT fabricated decimal pseudo-precision.
5. **Plan selection**: choose a set of feasible compatible actions, not necessarily exactly one. Prefer an information-gathering step before irreversible intervention when uncertainty high; include ranked rejected alternatives and reason.
6. **Approval**: plan may propose tasks but never mark a physical action performed. Only confirmed Vineyard/Winery command can do so.

## Example (no invented probabilities)
Input: sugar trend unclear, last temp valid but old, fermentation suspected slow. Output: primary `MEASURE temperature and sugar again`, alternative `OBSERVE with specified next window`, deferred `PROCESS_ADJUSTMENT` requiring new data, unknown event probability, explicit evidence and needed additional fields. A forecast of external temperature with provider-generated probability may be shown with provenance, but **do not imply it is the probability of treatment effectiveness**.

## Learning and feedback
Plan feedback `accepted`, `rejected`, `performedElsewhere`, `outcomeObserved` is distinct from confirmed domain operation. Use expert-reviewed scenarios to compare against baseline. Do not retrain automatically from sparse self-labelled results and assume causality. Local history helps context/personalization; global study must be evaluated for climate similarity and source quality.

## Cost policy
L0/1 first. If rules and retrieved playbook suffice, don't invoke advisor. Choose cheapest empirically acceptable model, cap tool calls, max completion tokens, max USD/run/day/month, redact context logs. Paid web search is only on unresolved evidence gaps; curated known URLs favored. Retry one alternative approved provider only when data egress policy allows and time/cost budgets permit.
