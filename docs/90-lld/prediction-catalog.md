# MVP prediction catalog — business decisions before model sophistication

Status: **design contract; candidate models, not validated scientific formulas**. Owner: `Decisions`; source facts remain with `Weather`, `Vineyard`, `Winery`, `Observations`, `Knowledge`. See [incremental prediction](incremental-prediction.md) and [decisions](agents-and-decisions.md).

## Why this exists

The product must answer **what is the next feasible action, when, and why**. A Prediction Definition is not the same as an LLM prompt: it declares which changes matter, the time horizon, the estimator and how to evaluate it. New data only invalidate dependent scopes. No invented risk percentages or universal treatment thresholds.

## Prediction definition manifest

Every definition must have these fields and a versioned code (immutable once used in a saved plan):

| Field | Semantics |
|---|---|
| `code`, `version`, `owner` | Stable ID; semantic version of algorithm/interpretation; owning module |
| `subjectKinds` | `Vineyard.Block` or `Winery.WineBatch` initially |
| `horizon` | Absolute evaluation horizon or relative window resolved at run start |
| `dependencies[]` | Domain event, changed fields, mapped subjects, optional weather window / knowledge tags |
| `minRequiredFacts[]` | Required input; failure yields `INSUFFICIENT_DATA`, not risk=0 |
| `featureExtractorVersion` | Deterministic aggregation with units, time zone and as-of semantics |
| `localEstimator` | Rule or model identity; test fixture and provenance; can return `UNKNOWN` |
| `materialityPolicy` | Threshold crossing, change in feasible action, expiring deadline or evidence quality |
| `refreshTtl` | Validity period; periodic expiry even without incoming facts |
| `advisorPolicy` | Optional role, allowed tools and trigger conditions after L0/L1 |
| `failurePolicy` | Retain old result as `STALE` and surface limitation; no silent fallback |

Changing scientific thresholds or transfer assumptions creates a new **definition version** and requires retrospective evaluation, not a silent configuration update.

## P-001 — Frost exposure and protective readiness (vineyard)

- **Subject:** each block; evaluate for next 24–72h, and explicitly for frost-sensitive observed phenological stage.
- **Inputs:** `WeatherForecastPublished` (`air_temperature`, wind, optional humidity, frost warning), `PhenologicalStageConfirmed`, `VineyardProfileChanged` (location/altitude/protection capabilities), confirmed recent vineyard activity.
- **L0:** validate forecast run provenance and locality; check valid timestamps and freshness; select forecast version known at `asOf`.
- **L1:** expose forecast range, proximity to a *configurable, source-justified* temperature risk boundary and data gaps. Without an on-site station, the model must mark microclimate uncertainty; do not extrapolate 2m forecast directly to bud temperature.
- **L2:** run Vineyard Advisor only when a time-critical choice exists, phenological applicability is uncertain or action feasible-set changes. Produce a verification/monitoring recommendation before costly protective action when justified.
- **Candidate actions:** local temperature check, inspect stage, prepare available protection, defer/observe, escalate; never claim protection equipment is available without domain evidence.
- **Success evidence:** human-reviewed timing and action plausibility; external observed temperatures are evaluation outcome only, never future input to a historical prediction.
- **Unknown:** no numeric probability unless an independently justified weather/probability model provides one with calibration/provenance.

## P-002 — Vineyard next-task readiness (vineyard)

- **Subject:** each block/cultivar group; rolling next 7 days.
- **Inputs:** confirmed operations, observed phase, applicable procedures, source rights and relevant weather windows.
- **L0:** recover last *confirmed* operation and outstanding task; do not treat unconfirmed narrative proposals as completed work.
- **L1:** determine candidate tasks based on phase/procedure applicability, preconditions and timing windows; `UNKNOWN` if observations or dated reference data are missing.
- **L2:** consult Vineyard Advisor only for meaningful trade-offs (weather window vs labor vs alternative action), conflicts between credible sources or a safety-sensitive task.
- **Candidate actions:** measure/inspect, perform allowed activity, postpone, no action; show prerequisite and legal-label check for any proposed plant-protection product.
- **Success evidence:** expert-acceptable top recommendation, no duplicated just-performed operation, appropriate alternative and time window.

## P-003 — Fermentation progress / next cellar step (winery)

- **Subject:** `Winery.WineBatch`, not vessel; rolling next 24–72h for active fermentation.
- **Inputs:** `WineBatchStateChanged`, `WineryOperationRecorded`, `ObservationRecorded|Corrected` for batch (temperature, sugar, pH / acidity when present), and vessel occupancy as-of measurement time; relevant source publications.
- **L0:** validate unit/method/time/quality; do not conflate pH, titratable acidity or sugar units. Ambiguous equipment occupancy must not bind a reading to the wrong batch.
- **L1:** compute available trend on observations known at `asOf`; no diagnosed stuck fermentation from absence of foam or a lone reading; signal missing trend/sample.
- **L2:** run Winemaking Advisor only on significant trend/state changes, conflict in evidence or an impending cellar decision.
- **Candidate actions:** re-measure temperature/sugar, verify sample, observe over defined window, propose process intervention **only with verified preconditions and approval**.
- **Success evidence:** no unsupported chemical dosage or wine safety assertion; reversible verification ahead of irreversible action where evidence weak.

## Triggers, scope and minimization

| Change | Affected definitions | Explicitly unaffected |
|---|---|---|
| New forecast for mapped vineyard location | P-001, weather-window part of P-002 | P-003 unless cellar temperature comes from that input by explicit contract |
| New phase in block A | P-001, P-002 on block A | Other blocks and all cellar batches |
| Confirmed vineyard operation in A | P-002 (and P-001 only if protection/readiness affected) | Winery; other blocks |
| Corrected sugar reading in wine batch X | P-003 on X, including backdated trend correction | Vineyard; batch Y |
| New knowledge index with cultivar + stage tags | Matching P-001/P-002 or P-003 by topic | Global recompute by default |
| New unknown knowledge topic | Bounded coverage-audit, retain explicit uncertainty | Unconditional paid scan over all subjects |

**Weather updates must be compared by effective forecast window, not payload hash alone.** Source metadata-only changes should not trigger AI. Dedupe by prediction scope and desired revision; critical deterministic checks bypass normal debounce. Execution while host is off resumes from durable queue; no always-on safety claim.

## Core test vectors

1. 30 one-minute weather readings, no threshold/choice change → updated state, **zero** paid calls and one coalesced scope job.
2. Forecast changes from safe to potentially frost-threatening overnight; phase observed as susceptible → P-001 invalidated for affected blocks only, time-critical local path.
3. No phase observation → `INSUFFICIENT_DATA` / inspect phase; no authoritative protective action.
4. Backdated correction to sugar trend received today → P-003 recompute now, historical `asOf` replay remains unchanged.
5. Two vineyard actions form prerequisites, not alternatives → plan may recommend both in the correct order.
6. Provider reports no probability → `eventProbability=null`, do not create a percentage in LLM or UI.
7. New data arrives during expensive reasoning → compare-and-swap fails; obsolete run cannot publish as `CURRENT`.
8. Knowledge rights changed to DENY → previously retrieved private snippet must not leave local runtime on subsequent advisor invocation.

## Remaining decisions before implementing formulas

Precise phenological coding convention, vineyard block location precision, equipment inventory, local reference thresholds and agronomic source validity must be decided from actual project data and specialist review. **Do not code generic freeze, pest or fermentation intervention thresholds as scientific truth.**
