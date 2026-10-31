# MVP domain journeys and acceptance slices

Status: Gate 2 accepted. Implement **three** reference journeys before optional integrations; stay within local .NET + PostgreSQL, a cheap vendor-model gateway and 1–2 books.

## Journey 1 — Forecast changes after vineyard action

1. User imports a note saying pruning/spraying/other work was carried out on a specific block, on a specific date.
2. Connectivity stores original note; extraction may propose an interpreted command, but missing subject/date requires clarification or review.
3. Vineyard commits `VineyardActivity` with immutable audit trail and emits change event.
4. Decisions identifies impacted prediction types and invalidates only relevant plan scope.
5. Weather and curated evidence are consulted if useful; local deterministic scoring preferred.
6. New plan revision explains why earlier recommendation changed, dates, evidence, alternative actions and what further measurement is required.

**Acceptance:** no unrelated cellar analysis; no duplicate action for reimport of the same note; no invented operation from uncertain extraction; plan revision recorded.

## Journey 2 — Winery note and next step

1. Create batch and identify current vessel; record fermentation status and contextual notes (date, observations).
2. Winery owns operations (transfer, addition, racking); Observations owns raw Brix/pH/temperature when available.
3. Context Builder retrieves relevant batch state, recent observations, applicable evidence with page citations.
4. Winemaking advisor proposes two or more candidates when appropriate, including 'measure first' or 'do nothing'.
5. Decisions performs constraints/sequence checks, stores decision plan and its versioned inputs; CLI/HTTP report renders it.

**Acceptance:** batch is independent of tank; numeric success probabilities are not fabricated; answer distinguishes source evidence from an expert inference.

## Journey 3 — Weather run changed

1. Local sync fetches weather forecast for configured vineyard location and persists provider-issued run (not overwritten by newest values).
2. Weather computes affected time windows; Decisions updates impacted field risks.
3. No semantically significant change: update freshness/checkpoints only; **zero** expensive AI calls.
4. Significant change: regenerate scoped plan; compare old/new actions; render update.

**Acceptance:** replay from stored issued runs reproduces assessment; no hindsight leakage in historical tests; only impacted blocks/predictions recomputed; available offline data labelled stale if out-of-date.

## MVP evaluation baseline

- Build 30–50 representative gold scenarios (including missing/contradictory data, regional mismatch, and unsafe/unlicensed recommendation paths).
- Evaluate retrieval Hit@5, factuality, action acceptability, deadline rationality, evidence citations, stale-data behavior, unnecessary AI calls, monthly token spend.
- Tests compare baseline plain LLM, books-only, books + curated web, contextual operational data, region-aware evidence, and full decision workflow.
- Hard gate: no invented quantitative confidence/probability; clear data-absent behavior; critical safety validation independent of LLM.

## Explicit MVP exclusions

No real-time IoT, no MFA/public internet, no microservices, no machine-actuated treatments, no elaborate UI, no subscription provider for weather, no live 24/7 guarantees, no automated email required, no free-form internet crawling, no on-device GPU requirement.
