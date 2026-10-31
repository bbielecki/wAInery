# Implementation test strategy — business-quality gates beyond compilation

Owner: **Independent QA**, assisted by Integration Owner and human domain reviewer. This is a **target plan**; only the solution build and architecture project-reference smoke check exist in CI currently. Do not represent these test levels as already implemented. [SDLC](../80-delivery/agentic-sdlc.md) | [Evaluation protocol](../90-lld/evaluation-protocol.md).

## Test pyramid — deterministic first

| Level | Scope / examples | Runner and timing | Must block what? |
|---|---|---|---|
| **Fast unit** | Aggregates (confirmed vs proposed operation, vessel occupancy, unit conversion), materiality predicates, ranking constraints, time windows, failure/retry eligibility | `dotnet test` module suite, on every PR (once created) | Affected module PR |
| **Contract** | Compiling C# ports; producer/consumer DTO examples; five JSON schemas; unknown field behavior; event version and error semantics | `tests/Contracts`, schema validator and .NET compile | Changes to producer, consumer, or shared contract |
| **Persistence/integration** | PostgreSQL + pgvector schema up/replay, indexes/FKs, append-only/version definitions, outbox atomicity, idempotent inbox, correction logic, batch-vessel occupancy constraints | Testcontainers using **real PostgreSQL** on CI and local when available | SQL / storage PR; final Wave 0 gate |
| **Architecture** | No illegal project references or cycles; no writes to foreign module schemas | Existing `tests/Architecture` + additional runtime/SQL ownership checks | Every PR |
| **Component** | Weather provider parsing/known-at; document chunk/citation resolution; source claims drafts; LLM egress/budget limits | Local deterministic fixtures and fake services; provider smoke test opt-in | Owning module PR |
| **Cross-module vertical** | Weather -> canonical forecast -> scoped invalidation -> decision plan; winery observation -> batch plan; confirmed activity -> next plan | `tests/Integration`, no remote LLM needed for core logic | Integration PR and MVP release |
| **AI/RAG eval** | OCR quality, hybrid lexical/vector retrieval, Polish contextual applicability, cited claims and feasible alternative actions | `tests/Evaluation` + 30–50 independently reviewed cases; isolate live API cost | Decision-quality release gate (not every code PR) |
| **Security and rights** | Prompt injection, denied unapproved external data egress, no protected data in telemetry/PR/fixtures, tool permissions | Negative policy tests, static scan, manual check | Any relevant source/model/agent/CI PR |
| **Performance and spend** | No AI for irrelevant event, bounded contexts, duplicate coalescing, run latency, cost per plan and budget exhaustion | Deterministic fakes + explicit metered trial | Decisions/AIExecution PR and MVP release |

## Example mandatory scenarios

| ID | Input / change | Required assertion |
|---|---|---|
| T-WX-01 | New forecast for block A only | Impacted block predictions become stale/recomputed; unrelated winery batch untouched |
| T-WX-02 | Provider forecast arrives late | As-of replay uses only facts available by decision time; no look-ahead |
| T-OP-01 | Narrative "I sprayed today" is imported | **Proposal**, not a confirmed Vineyard operation until user confirms |
| T-WIN-01 | Wine moves from tank #1 to #2 | Batch identity preserved; vessel readings attributed by historical occupancy |
| T-OBS-01 | Unknown unit or uncalibrated sensor | Quality state preserved/flagged; no silent conversion into authoritative fact |
| T-DEC-01 | Irrelevant Brix event for a different batch | **Zero paid AI**; unrelated prediction plan remains valid |
| T-DEC-02 | Two racing recomputes on same scope | Older input revision cannot overwrite newer result; retry converges |
| T-DEC-03 | Duplicate outbox delivery | No duplicate user action plan or performed operation |
| T-DEC-04 | Model suggests 87% action success without evidence | Reject / report unknown, not display a fabricated percentage |
| T-KNW-01 | Private scanned book evidence selected | Default external egress **denied**; allowed local retrieval still cites source |
| T-KNW-02 | Web text contains tool/prompt instructions | Treated as untrusted evidence, does not invoke unauthorized tools |
| T-AI-01 | LLM API unavailable or budget exhausted | Safe degraded decision state; local deterministic risk assessment still runs |
| T-EVAL-01 | French vineyard article conflicts with local phenology | Transferability caveat; no assumption foreign thresholds fit Polish season |
| T-EVAL-02 | Safest action is no action or take a measurement | Rank includes low-intervention alternative, not forced chemical procedure |

## Test design conventions

- **Synthetic data only in Git**: fake blocks/batches, generated weather and fictional book sources. Real private books/notes stay in local ignored folders. No embedding corpora or full proprietary passages in CI artifacts.
- **Temporal determinism**: inject clock; fixtures include `observedAt`, `receivedAt`, `knownAt`, `asOf` where relevant. Hidden future outcomes must never reach model context before scoring.
- **LLM-independent unit and integration tests**: fake `IReasoningExecutor`, fake forecast provider, deterministic retrieval responses. External API smoke tests require explicit opt-in and spend cap; they are not required for ordinary PR CI.
- **Golden scenario rubric**: accept multiple defensible action plans; record forbidden outcomes. Human independent review required before acceptance; an LLM cannot be its own sole grader.
- **Property/invariant tests** where useful: operation confirmation cannot be bypassed, time window always positive, probability unknown is distinct from 0, revision increases monotonically, no cross-workspace reads.
- **Reliability tests**: crash between canonical write and event publish, lease expiration, task replay, stale cache/prediction and retry DLQ handling. Verify no silent data loss.
- **Backward compatibility**: each published contract has a producer fixture, a consumer fixture and explicit versioning. Breaking contracts require coordinated producer/consumer migration.
- **Rights tests** must distinguish storing a private scan locally from sending protected text to an external model or repository; both are governed separately.

## Local commands: real versus planned

**Currently executable from repo scaffold** (requires .NET 10 SDK):

```sh
dotnet restore wAInery.slnx
dotnet build wAInery.slnx --configuration Release
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
```

**Once test projects have been implemented in Wave 0/1:**

```sh
dotnet test wAInery.slnx --configuration Release
# Run integration tests in tests/Integration against Testcontainers PostgreSQL + pgvector
# Run tests/Evaluation separately with pinned fixture/model/prompt versions and explicit cost budget
```

`dotnet test` is a *future command*, not proof such test projects exist today. The current GitHub Actions only checks build and reference boundaries; extend it incrementally rather than adding heavy infrastructure before writing tests.

## Release gates: quality of product, not model confidence

- Functional: accepted inputs → correct owner state, valid relevant predictions, actionable alternatives, time windows and source evidence.
- Safety: **zero critical forbidden recommendations** in the reviewed gate set; report evaluation coverage and reviewer disagreements. Never generalize this to guaranteed field safety.
- Retrieval: evaluate Recall@5 and source applicability on the same held-out fixtures; don't hard-code quality percentages without measurements.
- Economics: measure full workflow `CostPerAcceptedPlan`, distribution and quota behavior. Re-running all tests must not unexpectedly incur provider charges.
- Availability: local MVP may be offline; after restart durable data eventually catches up, with predictions visibly stale until recomputed.
- Extension: adding a measurement type or AI provider must not require destructive canonical-data migration.

Do not change a threshold just to make tests pass; document whether changing tests is correcting faulty expectations, updating policy, or masking a regression.
