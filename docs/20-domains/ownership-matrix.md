# Data ownership and authority matrix

Status: Gate 2 accepted. `Owns` means exclusive permission to mutate **canonical** data; other contexts may keep rebuildable projections.

| Concept / record | Canonical owner | Consumers | Important rule |
|---|---|---|---|
| Workspace identity tag | Platform minimal primitive | All | Scope field now; no centralized identity runtime in MVP |
| Vineyard, block, planted varieties | Vineyard | Decisions, Weather, Observations (subject validation) | Varieties are planted facts, not advice from LLM |
| Phenological stage (approved) | Vineyard | Decisions | Derived suggestion from observation is never automatically approved |
| VineyardActivity, intervention status | Vineyard | Decisions | Explicit planned/performed distinction, author and event time |
| Batch, vessel, stage, custody/genealogy | Winery | Decisions, Observations | Batch not permanently identical to tank |
| WineryOperation, status and results | Winery | Decisions | Batch operations include purpose and performed timestamp |
| Raw intake receipt (user note / webhook payload / PDF upload envelope) | Connectivity | Target owner through import | Immutable receipt and checksum; Knowledge becomes canonical owner of accepted book/document content, not Connectivity |
| MeasurementType, Observation, unit, quality | Observations | Decisions, Vineyard, Winery, Weather | Distinguish measured/inferred, method and observation timestamp |
| ForecastRun, forecast interval, forecast confidence | Weather | Decisions | Save `issuedAt` + provider + valid interval; avoid hindsight leakage |
| Document, document version, OCR, chunk, evidence span, rights | Knowledge | Decisions and permitted AI tools | Public status does not imply re-use rights; private derived spans inherit constraints |
| Prediction, RiskAssessment, candidate action, DecisionPlan | Decisions | API/CLI, later Notifications, Evaluation | Numbers only from verifiable probabilistic model; no fabricated percentages |
| Decision feedback (accepted/dismissed/executed ref) | Decisions | Vineyard, Winery, Evaluation | Feedback does not by itself prove action execution; owning domain confirms it |
| Connector config, sync cursor, raw import envelope | Connectivity | Platform operations | Never owns downstream normalized business meaning |
| Model selection, token usage, agent execution trace | AIExecution | Decisions, Evaluation | Traces must exclude protected source content by default |
| Golden test case, eval score, cost regression | Evaluation harness | Engineering / CI | Experimental fixtures, not production aggregate |
| Notification delivery attempts | Future Notification module | Owner/UI | Deferred from MVP; no separate domain table necessary now |

## Canonical vs raw vs projected

1. **Raw**: intake receipt, timing and checksum are owned by Connectivity; accepted document bytes, OCR lineage and source spans are owned by Knowledge. The intake receipt may refer to the same stored blob; only Knowledge governs long-term canonical document lifecycle.
2. **Normalized facts**: domain command or Observation, each with exactly one canonical owner.
3. **Derived feature/state**: read-only projection; rebuildable and tagged with source versions.
4. **Prediction/plan**: Decisions-owned derived artifact with complete provenance and policy/model version.
5. **Rendered report**: representation of a stored DecisionPlan, no second source of truth.

## Cross-context subject references

Use a stable discriminated identifier conceptually `SubjectRef = { WorkspaceId, SubjectKind, SubjectId }`, where `SubjectKind` initially `VineyardBlock` or `WineBatch` (optionally `Vessel` or `Vineyard`). At the boundary, resolve existence via the owner; avoid unbounded polymorphic IDs and hard database foreign keys across owners. For vessel readings, record measurement target (`Vessel`) **and** capture resolved contained batch at observation time when available; never reinterpret past measurements against the tank's current contents.

## Versioning and replay

- Source payloads immutable, normalized events idempotent (source ID + external message ID/hash).
- Domain operations can be corrected by compensating amendment, with a reason, not by altering original history invisibly.
- Derived plans include input revision references; recomputation creates a new plan revision, it does not mutate historical provenance.
- Embedding model/index version is distinct from document version; re-indexing is allowed without domain migration.
- Future adding sensor types should be metadata/config + optional quality rules; adding genuinely new business semantics may require additive schema migration.
