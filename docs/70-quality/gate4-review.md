# Gate 4 LLD baseline — retrospective architecture review

**Scope:** documentation and SQL blueprint on `main` before extension docs; not a code or running infrastructure review.

## Verified architectural properties

- Eight approved modules: three core, three supporting, two platform; `Decisions` owns predictions/plan, `AIExecution` owns model execution.
- One local .NET process, PostgreSQL+pgvector; domain owner write + durable outbox; at-least-once delivery and idempotent business effect.
- Public contract IDs are opaque strings; PostgreSQL internal canonical IDs can be UUID, resolved by owning module.
- History replay uses availability known at `asOf`, not only effective or observed timestamps.
- Book rights default to private storage and denied provider egress; LLM cannot directly mark physical tasks as performed.

## Findings / disposition

| ID | Severity | Finding | Disposition |
|---|---|---|---|
| R4-01 | High | SQL weather `issued_at` was already nullable, but `issued_at_basis` lacked a consistency check and lookup index used `issued_at` rather than known-to-system `retrieved_at` | Corrected in extension: consistency check for provider issue timestamp/basis and retrieval-time index |
| R4-02 | Medium | Previous `README` links to `gate4-review.md` that did not exist | This file restores traceable review evidence |
| R4-03 | High | Reference SQL is **not an executed migration**, with vessel occupancy overlaps and mixed-workspace correction constraints still not enforced | Open implementation blocker: add constraint or command-level check AND real Testcontainers tests before domain merge |
| R4-04 | Medium | Current MVP quality thresholds are proposed, not empirically observed | Explicit benchmark/reviewer protocol created; no claimed accuracy |
| R4-05 | High | Many proposed .NET public-port names have no compilable code/contracts yet | Wave 0 must freeze executable contracts and proof before parallel modules |
| R4-06 | Medium | Need explicit Polish applicability and no artificial probability | Added evidence policy and prediction catalog |

## Status

**Architecture baseline accepted for documentation**, with known implementation blockers. No one has run a real PostgreSQL DDL migration, .NET compilation or AI evaluation as part of this review. A later review must provide test command, environment and actual output for each requirement before any statement that MVP works.
