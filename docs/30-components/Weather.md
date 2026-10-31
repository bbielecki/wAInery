# Weather — component boundary proposal

## Components / ownership
- `ForecastCatalog`: immutable provider-issued forecast runs, target location/time and issued/retrieved/valid times; raw response pointer maintained through Connectivity.
- `ForecastDiff`: compare versioned runs by location + time window + relevant variable. Emits semantically affected windows, not one event per provider point.
- `WeatherReadFacade`: latest usable forecast for a scope as-of timestamp, freshness and coverage; no implicit observed fact.

## Public contracts
`AcceptForecastRun(sourceId,locationId,issuedAt,receivedAt,points,sourcePayloadRef)`; `GetForecastWindow(workspace,location,asOf,from,to,variableSet)`; returns explicit `FRESH|STALE|MISSING` and provider provenance.

## Events
`WeatherForecastPublished`, metadata including location, range, changed variables, run/version and diff status. Decisions may ignore non-material changes using its own risk policy.

## Data/storage
Schema `weather`: `locations`, `forecast_runs`, `forecast_points`, `outbox`; index by `(workspace,location,valid_at,issued_at)`; no mutation of prior runs. Forecast resolution/altitude metadata when provider exposes it.

## Failure, tests, evolution
Provider timeout: preserve last run with stale label; never invent temperature, never overwrite issuedAt. Backtests cannot query forecast issued after scenario as-of. Future on-site station observation goes to Observations, not ForecastCatalog; comparison layer added later.
