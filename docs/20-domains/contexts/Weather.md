# Weather bounded context

**Class:** Supporting domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Weather/`.

## Purpose
Versioned meteorological forecasts and local site exposure/context used for vineyard decisions. Avoid confusing forecast snapshots with on-site measurements.

## Owns
`WeatherLocation`, `WeatherProvider` (semantic provider identity; endpoint credentials/sync belong to Connectivity), `ForecastRun` (provider-issued timestamp and retrieval timestamp), `ForecastValue/Interval`, `ForecastFreshness`. Later site-specific forecast adjustment metadata.

## Business invariants
- Never overwrite old forecast runs: preserve issuedAt and validity interval for backtesting, compare providers and model revisions.
- Forecast valid time, acquisition time and provider run time are distinct.
- Forecast values must track units, elevation/location and spatial resolution when known; regional forecast applicability may be weaker than on-site data.
- Weather-station actual readings go to Observations, not a forecast table.
- If no fresh data, return explicit stale/unknown status rather than false precision.

## Consumes
Weather API payload via Connectivity; curated vineyard site/block coordinates from Vineyard through read ports (no cross-owner writes).

## Publishes (illustrative)
`WeatherForecastPublished`, `WeatherForecastValidityChanged` with affected location and time range.

## Excludes
Phenological truth, agricultural actions, model-risk probabilities and decision plans.

## MVP acceptance
Free-weather adapter, store immutable forecast runs, compare material forecast changes by location/time, query most recent valid forecast as-of timestamp, label stale/failed fetch.
