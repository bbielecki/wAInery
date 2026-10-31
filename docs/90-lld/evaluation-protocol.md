# Reproducible MVP experiment protocol — quality / tokens / cost

Status: implementation specification for `tests/Evaluation` and an optional local CLI. Owner: Evaluation/Integration Agent, with separate domain reviewer. This expands [acceptance](evaluation-and-acceptance.md). **Do not claim accuracy until measured.**

## Hypotheses to test

- H1: including confirmed domain history improves action appropriateness over general LLM response.
- H2: curated Polish-relevant evidence and measured transferability improve locality-aware decisions vs raw books-only RAG.
- H3: hybrid retrieval outperforms lexical-only and vector-only for Polish/English viticulture vocabulary at similar cost.
- H4: incremental L0/L1 materiality gates reduce external AI invocations without missing materially changed decisions.
- H5: cheapest acceptable model route meets user-defined quality bar for normal cases; higher-cost route only for hard cases.

## Synthetic fixture contract (not an API v1 change)

Example shape for file `tests/Evaluation/scenarios/MVP-WX-001.json` (all IDs synthetic, no proprietary book passages):

```json
{
  "scenarioId": "MVP-WX-001",
  "revision": 1,
  "domain": "VINEYARD",
  "asOf": "2026-04-18T07:00:00Z",
  "subject": {"module":"Vineyard","kind":"Block","id":"test-block-A"},
  "givenSources": ["fixture:forecast-run-2", "fixture:observed-phenology"],
  "hiddenFutureOutcomes": ["fixture:observed-actual-weather-18-04"],
  "expected": {
    "requiredFindings": ["potential_frost_exposure", "uncertain_microclimate"],
    "allowedActionKinds": ["MEASURE", "OBSERVE", "PREPARE", "ESCALATE"],
    "forbiddenClaims": ["certain_damage", "fabricated_event_probability"],
    "requiresEvidenceRef": true,
    "requiresTimeWindow": true
  },
  "assessment": {"expertRequired":true, "costLimitUsd":0.05}
}
```

Keep fixture IDs separate from actual module IDs. `hiddenFutureOutcomes` are **never passed** to Context Builder, adapters, models or the evaluation request before the as-of time; they may be used by scorer **after** an immutable prediction run is complete.

## Independent human review rubric (0–3 per dimension)

| Dimension | 0 = fail | 1 = weak | 2 = adequate | 3 = strong |
|---|---|---|---|---|
| State interpretation | Fabricates or misses key fact | Material omission | Correct with minor caveat | Complete and provenance-aware |
| Local applicability | Unsafe/unjustified transfer | Major mismatch caveat missing | Caveat and suitable context | Evidence strongly matched to locality |
| Action feasibility | Violates hard constraint | Omits major prerequisite | Feasible action, known uncertainty | Good compatible plan and alternatives |
| Time and re-evaluation | Incorrect deadline | Vague timing | Appropriate time window | Time window plus explicit recheck trigger |
| Evidence fidelity | Fabricated reference | Relevant but weak attribution | Resolvable evidence | Multiple balanced relevant sources |
| Calibration/humility | Invented probability | Overconfident | Unknown explicitly signaled | Uncertainty and information gain handled |

For a scenario, record every acceptable plan instead of one exact answer. Different feasible `MEASURE`-first or `OBSERVE`-first plans may both pass. A safety-critical forbidden action fails the case regardless of weighted score. Two reviewers evaluate critical scenarios independently; disagreements are resolved with recorded rationale. Model cannot score itself as sole judge.

## Outcome metrics and computation

- `Recall@5 = count(queries whose at least one gold evidence ID appears in first five) / count(eligible queries)`.
- `AcceptableTopPlanRate = accepted top-ranked plan scenarios / reviewed scenarios` with bootstrap interval if enough cases.
- `CriticalSafetyViolationCount` is absolute; release gate requires **zero**, but report count and fixture coverage separately.
- `UnnecessaryAiCallCount` is calls made for events explicitly labelled irrelevant; target zero on deterministic fixtures.
- `StalePublicationCount` is invalid revisions published as `CURRENT`; target zero on race fixtures.
- `CitationValidityRate` checks referenced document version + page range/URL, plus rights class.
- `WallClockLatencyP50/P95`, `TokensIn/Out`, `CostUsd` both per scenario and aggregate; include failed attempts and paid tools.
- Forecast probability Brier scoring only on **labelled probabilistic events** with model provenance; not on LLM subjective confidence and not on tiny uncalibrated samples.

## Reproducibility manifest per run

`EvaluationRun`: `runId`, `datasetVersionHash`, `scenarioIds`, `codeCommit`, `schemaMigrationVersion`, `promptTemplateVersion`, `modelProvider+exactModelId`, `embeddingSpaceVersion`, `indexPublicationId`, `retrievalConfigHash`, `predictionDefinitionVersions`, `weatherProviderFixtureHash`, `randomSeed?`, `pricingTableVersion`, `startedAt`, `finishedAt`, outputs, costs, failure categories and reviewer IDs.

If provider nondeterminism cannot be eliminated, run repeated trials for selected cases; report the spread, not just the best response. Record exact model IDs and input hashes without saving protected raw text into Git. Never reuse prompt caches across rights scopes.

## Experimental matrix and stopping rule

Run identical scenario set through variants:

1. **B0:** no RAG, facts-only small model.
2. **B1:** book evidence, hybrid retrieval.
3. **B2:** books + curated web (region-applicability scores).
4. **B3:** B2 + confirmed operational history and as-of forecast.
5. **B4:** B3 + cheap L0/L1 materiality gate (vs always-LLM control).
6. **B5:** same as B4 on alternate model route / reranker.

Use fixed held-out scenarios and a development set; do not tweak thresholds based on held-out failures before recording a new dataset revision. Select the Pareto-acceptable option on quality **and cost**, not the model with highest unqualified subjective score. If baseline quality misses release gate, perform error attribution: source missing → OCR → index → retrieval → context → model → decision constraint → rank.

## First runnable milestones

- Import synthetic fixtures and a small permitted evidence corpus (not scanned copyrighted text).
- Support deterministic `--as-of` replay with provider fixture substitution and fully offline execution mode (stub reasoning).
- Emit `results.json`, `summary.md`, `usage.csv`; all outputs exclude proprietary content by default.
- Validate decision-plan schema and forbidden fields; fail on violations and on leakage/cost policy breaks.
- Publish a signed-off evaluation report before claiming the MVP predicts events accurately.
