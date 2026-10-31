# First MVP vertical slice — executable evidence chain

Status: implementation-ready **plan**, not working product. Scope: one locally stored synthetic vineyard block, one weather forecast run, one decision and one persisted local Markdown report. The experiment proves modular event semantics before investing in OCR and multi-agent sophistication.

## Sequence and ownership

| Step | Owner | Deliverable / integration proof |
|---|---|---|
| 0. Freeze shared contracts | Integration | JSON schemas, C# DTO/ports, errors, idempotency, clock, subject ID resolver, integration tests compile |
| 1. PostgreSQL foundation | Integration | Schema migrations on Testcontainers pgvector, one workspace, outbox/inbox/jobs, restart tests |
| 2. Vineyard source of truth | Vineyard | Synthetic Block A + observed phenological stage; confirmed update atomically emits event |
| 3. Weather and ingestion | Weather + Connectivity | Curated forecast fixture; immutable run, true issued time optional, valid-at windows, hash no-op |
| 4. Impact and local prediction | Decisions | P-001 rule inputs, mapping block to weather location, scope invalidation, no fabricated probability |
| 5. Feasible action plan | Decisions | Inspect/re-measure first where uncertainties; alternatives and time window, constraints and provenance |
| 6. Local output and feedback | Host + Decisions | Read plan, freshness status and render Markdown report, record confirmation separately |
| 7. Add Knowledge | Knowledge | Small allowed corpus, hybrid retrieval, citations and rights policy; compare baseline with no RAG |
| 8. Optional advisor | AIExecution | Only if materiality/uncertainty gate opens; enforce budget and provider egress; schema result validation |
| 9. Winery second slice | Winery + Observations | Batch X, measurement, performed operation, P-003 recompute; verify separate vessel history |
| 10. Evaluation | QA | Golden scenario suite, as-of replay, no future leakage, quality/cost report, critical gates |

## Milestones and independent PRs

- **M0:** one green contract solution + actual database migration; do not merge domain PRs against speculative interfaces.
- **M1:** deterministic weather→invalidation→prediction→plan→report entirely offline, no LLM required.
- **M2:** use eligible public synthetic evidence and compare no-RAG vs hybrid-RAG, zero copyrighted content committed.
- **M3:** controlled AI proposal route, policy check and token ledger, materiality prevents unrelated calls.
- **M4:** own cellar batch measurements and operations, as-of correctness, multiple compatible actions.
- **M5:** evaluation 30–50 scenarios and human sign-off; do not claim production readiness without actual outcomes.

## Acceptance examples

- `forecast-hash-same`: two identical upstream payloads → one canonical published run or no-op by source policy; zero AI.
- `weather-changes-outside-risk-window`: immutable forecast run accepted but no material new recommendation; no mail (notifications outside MVP).
- `forecast-moves-into-risk-window`: affected block's prediction becomes stale; if as-of facts missing, recommended *measurement*, not fabricated certainty.
- `confirmed-work-done`: a human-confirmed operation changes history; duplicate note / repeat import cannot add a second performed operation.
- `outbox-crash`: crash after canonical commit before dispatch, restart consumes exactly once in business effect.
- `reasoning-race`: new observation arrives while provider call is in-flight; obsolete advice not published current.
- `private-evidence-denied`: private excerpt absent from provider payload, prompts log and report body; citation only as safe local ref.
- `cost-exhausted`: L0/L1 continues and report says optional reasoning skipped, but no unsupported conclusion is promoted.

## Evidence package for every milestone

Each PR includes a reproducible command, commit SHA, sample synthetic seed, relevant schema version, test results (or explicit not-run reason), expected queries/events, cost measurement and rollback. CI must run `tests/Architecture`, `tests/Contracts`, affected module tests and SQL integration tests when the runner supports containers.

## Do not add to MVP

No public auth, SaaS tenancy enforcement, on-site station, sensor fleet, notification service, always-on monitoring, Kafka, Redis, vector SaaS or separate agent microservices. Preserve extension ports only. A localhost-only implementation and intermittent runtime are deliberate, documented trade-offs.
