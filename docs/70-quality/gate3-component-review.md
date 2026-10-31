# Gate 3 component proposal — architecture review log

Date: 2026-10-31. Status: **proposed, awaits user approval**. Review nature: document/schema consistency, not implementation, legal clearance or scientific expert certification.

## Findings / corrections performed

| ID | Severity | Review issue | Resolution in Gate 3 |
|---|---|---|---|
| G3-01 | High | AIExecution could become second owner of decisions | DecisionCoordinator/selection stays in Decisions; AIExecution only supplies structured advice and usage |
| G3-02 | High | A model-generated draft diary entry might be treated as performed | Separate proposal/confirmation APIs in Vineyard and Winery, no direct AI writes |
| G3-03 | High | Outbox dispatch vs consumer transaction could lose invalidations | At-least-once dispatch, consumer dedupe and stale marking + job creation transaction, lease recovery |
| G3-04 | High | Frequent changed measurements cause excessive token costs | Scope + materiality + coalescing and zero-LLM nonmaterial branch |
| G3-05 | High | Late model response can publish plan based on stale input | InputRevision check before commit and reschedule on mismatch |
| G3-06 | High | `probability` floats could be invented by LLM | Nullable, auditable probability provenance with method/model/calibration; LLM may not populate without eligible source |
| G3-07 | High | Private OCR might escape via retrieved context/tool logs | Dual rights check in Knowledge and AIExecution; log metadata only; no provider switch that weakens rights |
| G3-08 | Medium | Cross-context code references cause cyclic dependency | Contract-only references, Host DI as composition root |
| G3-09 | Medium | Forecast issuedAt vs receivedAt causes hindsight bias | As-of requires both <= historical cutoff |
| G3-10 | Medium | Book upload bytes belong to two contexts | Connectivity receipt pointers; Knowledge accepted canonical document versions |
| G3-11 | Medium | Batch bound to tank makes later transfers unsafe | Separate BatchRegistry/VesselRegistry + effective-time occupancy |
| G3-12 | Medium | Notification as synchronous side effect of decision | Plan publication event, transport consumer deferred |
| G3-13 | Medium | Storing all domain facts as JSON impedes integrity | Relational domain core, extensible measurement catalog, JSONB only for optional metadata |
| G3-14 | Medium | Local app assumed continuously running | Stored jobs and resume on restart; no alert availability guarantees |
| G3-15 | Medium | Contract envelopes leak raw protected notes or book excerpts | Event only references IDs/version/changed fields; payload stays protected in owner |

## Validation required before Gate 3 approval

- `contracts/**` JSON Schema draft and examples validate.
- Local markdown relative links checked; eight module docs present, ownership matrix/diagram match.
- No actual code, paid infra, repo modifications or API calls to models included in this Gate 3 design.
- Pending user feedback on seams, outbox and draft-vs-confirm semantics.

## Deferred to Gate 4 full LLD

1. Concrete aggregates and state-machine invariants, exact SQL DDL/EF mapping, indexes/constraints, migrations.
2. Detailed dependency/impact rules, risk model versions and criteria for materiality by prediction type.
3. RAG benchmark configs, embedding dimensions/version, FTS Polish morphology approach, OCR QA, measured reranker value.
4. C# DTOs and compilable interfaces with unit/integration/contract tests and provider fallback behavior.
5. Reviewed business probability calibration/evidence definitions for each risk type.
6. Developer workflow details, separate agent assignments with test owners, cost budget fixtures.
7. Security review for local filesystem, API binding, provider egress and backup restoration.
8. Github branch/PR only when private repository access becomes available and after requested gates.
