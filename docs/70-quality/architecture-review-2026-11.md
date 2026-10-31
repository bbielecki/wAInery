# Architecture review — business fit, DDD boundaries and parallel delivery

**Review date:** 2026-11-01. **Reviewed scope:** current `main` LLD plus the documentation extension in PR #1 (`docs/mvp-prediction-evaluation-contracts`), which this branch builds upon. **Review type:** documentation / architecture / static-contract review, **not** a running system, expert agronomic validation, database execution or measured model evaluation.

**Verdict:** **retain the approved eight-module design.** It meets the **intended architectural** MVP direction, with a small set of concrete Wave 0 blockers; it does **not yet prove** recommendation quality or runnable implementation. Avoid new services/BCs solely to improve theoretical separation. See [Business HLD](../10-architecture/business-hld.md).

## 1. Business objectives and constraints

| Requirement | Design review | What would actually prove it |
|---|---|---|
| Relevant next action, **when**, alternatives and evidence | **Covered in design:** Decisions action ranking, dependencies, time windows, local report | Human review of 30–50 cases with allowed and forbidden decisions; evidence resolution |
| Recompute after each accepted change, narrowly | **Covered in design:** canonical outbox → scoped impact → L0/L1 → materiality-gated AI; snapshot CAS | Crash/replay, race, delayed data, unrelated-event zero-call tests on actual Postgres |
| Polish specificity; **not books-only** | **Covered in design:** history + weather + curated web + source transferability; rights and legal veto | Expert tests contrasting climates, cultivar/stage, outdated labels, book/web disagreements |
| Minimum cost private/local MVP | **Covered in design:** one .NET host, PostgreSQL+pgvector, local files and OCR, pay-per-use optional LLM | Monthly/token per accepted plan logs, startup/shutdown tests; no mandatory paid service |
| Add sensors, data sources, AI providers and UIs later | **Covered by ports/registries:** typed measurement + source versioning, model-neutral executor, read API | Contract tests for new measurement/provider and an additive schema migration |
| Safety, traceability and private book IP | **Covered in design:** owner-confirmed action, known-at snapshots, default-deny egress, no private texts in Git | Negative leakage tests, workspace checks, as-of tests and rights enforcement in code |
| Actual statistical predictions with probabilities | **Explicitly not proven:** L1 risk models/qualitative estimates defined, calibrated probabilities optional | Independent outcome data + model/calibration assessment before publishing percentages |
| Email/mobile notifications and 24/7 public reach | **Deliberately outside MVP** | Later release, not grounds for blocking local MVP |

**MVP interpretation:** model quality is an experiment, not a promised percentage. The first vertical slice (weather → change impact → local plan/report) validates plumbing and scope, **not** agronomic accuracy by itself. The full MVP gate requires additional winery/history and evidence scenarios.

## 2. Domain and bounded-context assessment

| Context | Assessment | Boundary to preserve |
|---|---|---|
| **Vineyard** — core | Cohesive business model of blocks, cultivars, phenology and confirmed activities | Do not use Observation as a surrogate for performed field work |
| **Winery** — core | Cohesive state of batch and vessel, operations and transfers | Batch identity independent of tank; occupancy resolved at observation time |
| **Decisions** — core | Broad but cohesive around one aggregate business outcome: prediction → feasible options → plan revision | Owns input impact, ranking, decision status; do not make AIExecution owner of decisions |
| **Observations** — supporting | Correct normalization for typed manual/IoT/lab measurements and quality | Observations are source claims, not canonical operations or forecasts |
| **Weather** — supporting | Appropriate separation of immutable forecast snapshots from measured weather | Provider/retrieval/valid times, known-at replay and locality |
| **Knowledge** — supporting | Proper ownership for private documents, RAG and provenance | Retrieval yields evidence, not current state; separate source rights and regional applicability |
| **Connectivity** — platform | Correct adapter/intake boundary; future devices plug in here | Raw receipts and proposals; no direct writes to another module's domain tables |
| **AIExecution** — platform | Correctly replaceable model/tool boundary | Returns untrusted candidates, never authoritative facts or probabilities |

**DDD judgment:** the six business/supporting contexts have useful language and data ownership. Connectivity and AIExecution are **platform modules**, not necessarily rich domain Bounded Contexts; this classification is already acknowledged and is not a defect. Do not split Decisions into separate Prediction and Planning contexts before performance, ownership conflict or product scale justifies it. Avoid a universal Operation aggregate shared by Vineyard and Winery.

### Cross-module dependency direction

- Shared **BuildingBlocks.Contracts** and each owner module's **Contracts** are the only permissible compile-time cross-module dependencies; no EF entities or private infrastructure types cross.
- **Decisions** depends on read ports/events of Vineyard, Winery, Observations, Weather and Knowledge. **AIExecution implements a Decisions-owned reasoning port via Host DI**; it should not require a source-level reverse reference.
- **Connectivity** accepts input and sends typed commands/proposals to owners. Only owners commit a canonical fact plus event atomically.
- No need to add a Notifications or Identity bounded context to local MVP. Future Notification consumers subscribe to plan changes without importing the Decisions domain implementation.

## 3. Parallel engineering viability

**Conditional YES:** seven modules can be implemented independently in Wave 1 **after a short serial Wave 0**; Decisions can build against fakes in parallel but full integration waits for stabilized owner read DTOs.

| Wave | Exclusive ownership | Merge gate |
|---|---|---|
| **0 — Integration Owner** | BuildingBlocks, canonical C# ports/DTOs, wire schemas, Host skeleton, migration runner, shared test fixtures | DTOs compile, schema examples pass, DB migration runs on Testcontainers, no conflicting contracts |
| **1 — Seven module owners** | Vineyard, Winery, Observations, Weather, Knowledge, Connectivity, AIExecution + own module tests | Each ports/events validated against frozen v1; no edits to other modules or central migration |
| **2 — Decisions owner** | Incremental registry, queue/CAS, context assembly, feasible action ranking and plan history | End-to-end mocked dependencies and real data-path integration |
| **3 — Integration/QA** | Three journeys + benchmarks, replay, crashes, rights, costs | Independent reviewer verifies cases, regressions, safety |

**No hard requirement for seven simultaneous developers:** work packages are independent enough for agents, but one integration owner must serialize common contracts and database migration assembly. One person can also implement workstreams sequentially. Division is designed for parallel **code ownership**, not seven microservices.

### Conflict-prevention rules

- Module agents write `src/Modules/<module>/**` and matching tests only. Changes to shared DTOs/events are proposed for integration-owner review first.
- **Registry schema ownership must be explicit:** `registry.workspaces` is bootstrap/platform-owned; `registry.sources` is managed only through Connectivity; `registry.measurement_types` only through Observations. Physical table location is not permission for other modules to write.
- Central reference SQL is an **integration-owned assembly artifact**. Prefer module-owned migration proposals applied in reviewed order; do not let seven agents modify `001_foundation.sql` concurrently.
- Contract consumers run fixtures from both producers and readers; the Integration Owner prevents method/name divergence and version breaking changes.

## 4. Findings — material items only

| ID | Severity | Observation with repository evidence | Recommendation / disposition |
|---|---|---|---|
| **AR-01** | **Before Wave 0** | C# sketches differ: `IReasoningExecutor` uses `AdviseAsync` in `ports-and-compatibility.md` but `ExecuteAsync` in `docs/40-contracts/catalog.md`; read methods and `SubjectRef.Workspace` shape also differ. None are compiled contracts. | **Documentation alignment in this PR:** catalog points to one canonical sketch; freeze note aligns `SubjectRef`. Integration Owner still must build actual compiled DTOs before independent branches. |
| **AR-02** | **Before real migration** | `decisions.prediction_definitions` has primary key `code` plus a `version` column, while the LLD requires simultaneous immutable definition versions. Existing `prediction_states` references code only. | **Reference SQL updated in this branch:** composite `(code,version)` key, state/run version FK, one-active-definition index. **Still open:** run a real PostgreSQL migration and transition/replay tests; enforce immutability of definition payload in the application or with a database trigger. |
| **AR-03** | **Before parallel DB edits** | Logical ownership is sound, but `registry.sources` and `registry.measurement_types` share a physical `registry` schema; central SQL may invite simultaneous edits. | **Ownership clarified in this review** and contract-freeze doc; central migration integration-only. No domain split needed. |
| **AR-04** | **Documentation hygiene** | `README.md` and `review-gates.md` still mention obsolete publication/approval status, and delivery traceability names only `BR-01..06` while the matrix defines 10. | **Correct in this PR.** |
| **AR-05** | **Evidence before acceptance** | SQL is a reference blueprint and C# ports are sketches; no actual Postgres integration run, compilation, expert-reviewed quality measurements or calibrated local prediction results are available. | **Open implementation gates**, already recognized by current docs. Keep honest wording; do not call MVP production-ready. |

**Not findings:** deliberate absence of MQTT, a dedicated notification service, advanced auth, cloud infrastructure, multiple running agents or trained local ML models. These are appropriate out-of-MVP choices.

## 5. Review decision and next checkpoints

- **Approve continued development on this architecture.** Business fit and separation-of-concerns are credible, with no need for structural redesign.
- **Do not announce Wave 0 completed** until the compiled contracts and actual Postgres migrations pass. Close AR-01 at compiled-port level and AR-02 at actual SQL migration/replay-test level before allowing full parallel persistence and Decisions integration.
- **Do not announce prediction success** until independent domain reviewers score real scenarios, including cases where a measurement/no-action is best and percent probability must remain unknown.
- Watch operating costs on accepted plans (not just token price), source rights, change impact selectivity and known-at replay. Reconsider architecture only if measurements reveal a specific bottleneck.

## Review evidence / limits

Documentation examined: business requirements, context map, ownership matrix, eight context definitions, component mapping, machine contract catalog and five JSON schemas, Prediction Catalog, Polish Evidence Policy, evaluation documents, incremental workflow, SQL reference, parallel assignment and delivery plans. Static JSON schema validation is possible; this review does **not** claim to have run executable migrations, CI, live AI calls or agronomic field trials.

**Review status:** architecture **accepted with pre-implementation conditions**, not an assessment that the finished business outcome has already been achieved.
