# G1-03a proposal — Weather and common command scope

Ticket #14 / milestone #10. Weather owns its contracts; Integration Owner owns
CommandContext and approves the first compiled form within human-approved Gate 1.
BR-01/06/09. Consumers: Connectivity (publish), Vineyard/Decisions (location/context).

No Weather public implementation exists. Preserve GetKnownForecastAsync name,
but include WorkspaceId and opaque location ID to satisfy the HLD. Immutable run
DTOs retain provider-issued time (nullable), issue basis, retrieved time, source
version, point valid times, variables and explicit units/nullable values.
Coverage/freshness and semantically changed windows remain separate from risk
materiality (Decisions). Queries use as-of plus valid window; station facts stay
with Observations. Public DTOs do not expose persistence UUIDs/entities.

Read ports use the existing Result<T> convention to distinguish an unknown
location (NOT_FOUND) from a known location without a forecast (MISSING). This
clarifies the pre-compilation sketch; there are no published C# consumers or
weather wire schemas to break. MissingWindows distinguishes coverage from age.

CommandContext mirrors existing LLD workspace, actor, idempotency, correlation,
expectedVersion conventions and supplies no implied confirmation of operations.
No wire schema or version changes; no actual forecast formula/provider/DDL here.
Boundary DTO tests precede code; full actual as-of/replay and atomic owner writes
remain Gate 2. Consumers use the compiled seam only after the contract PR merges.
