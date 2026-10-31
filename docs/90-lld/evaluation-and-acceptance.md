# LLD — MVP quality evaluation, baselines and gates

**Success definition:** recommend an appropriate **next action and time** for real vineyard/cellar context, with alternatives, uncertainty and evidence, at low average API cost. A nice chat response alone is not success.

## Gold scenarios
30–50 human-reviewed scenario fixtures, split: vineyard/weather (>= 12), cellar/fermentation (>= 12), changed history/recompute (>= 6), lack of data and contradictions (>= 6). Include Polish cultivar/microclimate/season relevance, regional transferability and legal plant protection constraints. Each includes snapshot `asOf`, only sources actually available at that time, expected risk/critical omissions, acceptable plans (not just one gold answer), forbidden actions, citation targets, expected next reassessment and max latency/cost.

Use **chronological dataset splits** for historical testing: no future rain observations, revised forecasts or subsequently written operations may appear as-of scenario time. Add adversarial fixtures for outdated labels, partial PDF, prompt injection, ambiguous note and wrong vessel occupancy.

## Baseline experiments
A. No RAG; B. Books only; C. books + curated web; D. + operational state; E. + local climate evidence; F. hybrid search; G. gated advisor vs always-LLM. Compare correctness against cost and latency. Keep same scenario versions and as-of snapshots.

## Proposed (not yet measured) quality gates
| Metric | Proposed threshold | Measurement |
|---|---|---|
| Retrieval gold citation found Top-5 | >= 85% | labelled evidence chunks |
| State extraction/recognition | >= 90% | field-level precision/recall |
| Selected top plan acceptable | >= 80% | expert review of multiple correct paths |
| Every operational recommendation grounded | 100% | plan schema + citation ref verification |
| Invented numerical probability | 0 | negative fixtures + schema/provenance checks |
| Severe unsafe advice in critical fixtures | 0 | manual hazard review, blocked releases |
| Unrelated event triggers AI | 0 | call counter in deterministic simulation |
| Recompute obsolete run published current | 0 | race tests with CAS |
| Materially unchanged plan spams notifications | 0 | semantic hash/hysteresis test |
| Model usage within configured budget | 100% | token ledger/guard test |

## Calibration
The LLM must not generate percentages as probabilities. For forecasts derived from statistical or domain models, store method, sample size/data basis, version, calibration status and evaluation reference; compare Brier score and reliability diagrams only when sufficient independently labelled outcomes exist. When unavailable use qualitative confidence separately. Decisions may rank without event/action success probability.

## Test pyramid
- Unit: domain state transitions, operation confirmation, source classification, quality validators, materiality, DAG and action constraints.
- Contract: all 5 JSON schemas + positive/negative fixtures, required fields, idempotency, stable serializations.
- Integration (Testcontainers Postgres): actual atomic outbox, concurrency lease, duplicate ingestion, FK and workspace isolation, as-of weather, embedding dimension.
- End-to-end: 3 MVP journeys and replay after simulated crash.
- Security: SSRF, local-only binding, prompt injection, no protected content leak/unauthorized egress, tokens absent from logs.
- Evaluation: independent expert review of gold scenarios, retrieval ablation and token spend.

`tests/Evaluation` fixture files must be synthetic or cleared for distribution; never copy real copyrighted book content into Git. Owner-produced observation history treated as private unless explicit consent.

## Ship/no-ship rule
Must pass contract/integration/security gates and minimum expert-reviewed sample. If the proposed quality thresholds fail, do NOT add more agents by default: measure attribution among OCR, retrieval, context quality, regional suitability, model and decision ranking; improve highest impact component first.
