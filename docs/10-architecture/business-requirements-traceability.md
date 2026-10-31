# Business requirement → component → acceptance test traceability

All components must preserve the **economic/decision objective**: trustworthy next action, credible timing, clear alternatives, Polish applicability, and only necessary compute. Reopen this matrix at every gate and review.

| Business requirement | Owning component(s) | Gate 4 test / measurement |
|---|---|---|
| BR-01 Recompute after any accepted change, not full recalc | owner outbox + Decisions.ChangeImpact + RecomputeScheduler | Changes to a single batch never trigger unrelated block/batch computations; stale visibility and jobs survive restart |
| BR-02 Multiple viable actions, ranking & dependencies | Decisions.ActionEvaluator + DecisionPlanner | Candidate set has alternatives, prerequisites, incompatibilities; best feasible action can be measure-first or no-action |
| BR-03 Polish climate/variety fit, not book-only | Knowledge.HybridRetriever + Decisions.ContextAssembler + Vineyard.ProfileRegistry | Gold scenarios with a foreign reference in conflict with regional data favor contextually justified interpretation; source quality and applicability separate |
| BR-04 Actionable report, status, time | Decisions.DecisionReadFacade | Plan contains subject, validity/status, ranked actions, timing with uncertainty, evidence and decision versions |
| BR-05 Minimum cost | materiality rule, ModelGateway budgets, local Postgres/OCR | 0 AI calls for immaterial updates; record token/$ per accepted plan and fixture; no new always-on service |
| BR-06 Future sensor sources without core rewrite | Connectivity.SourceRegistry + Observations.MeasurementCatalog | Introduce new known-type sensor by configuration and adapter, not new Vineyard/Winery tables |
| BR-07 Provider/model/UI replaceability | AIExecution.IReasoningExecutor + DecisionReadFacade + local HTTP | Mock and 2 different provider adapters satisfy same contract; no vendor SDK in core assemblies |
| BR-08 Private books/IP and safety | Knowledge.RightsPolicy + AIExecution.DataEgressPolicy + typed tools | Block local-only source egress and logs; reject unsafe/unconfirmed action |
| BR-09 Historic reproducibility | all owners versioned facts + Decision input fingerprint | Run 'as-of T' excludes forecasts/observations received after T; original plan remains auditable |
| BR-10 Minimal MVP but scalable | one host, one DB, durable outbox, module schemas | Local compose + restart/replay; no external broker or cloud cost; additive migrations tested |

## MVP benchmark: compare systems, not just chat fluency

Gold cases (30–50 initially) compare no-RAG, books-only, curated-web, operational context, Polish-context metadata and full Decision pipeline. The expert-facing evaluation labels include: correct facts, citation grounding, regional relevance, action acceptability, timing feasibility, recognition of missing data and no fabricated probability. Track retrieval hit@5, unnecessary AI calls, model cost per *accepted* plan, and safe-degradation cases.

## Scope decisions

- An update that cannot be evaluated now still creates durable changed-state/invalidation; absence of running local host is **not** uninterrupted monitoring.
- A risk probability may be absent. The system must not format qualitative ranking scores as probabilities.
- Local books may constrain egress; inability to send excerpts to a remote provider is handled with local analysis or explicit opt-in, never silent unauthorized sending.
