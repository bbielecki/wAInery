# G1-03a — Weather contract freeze work order

Milestone #10, ticket #14; BR-01/06/09; API budget 0; Integration Owner.
Owner of exported ports: Weather. Stage: Review. Upstream #13 merged.
Branch agent/weather/g1-03-weather-contracts.

Allowlist: Weather/Contracts, BuildingBlocks/CommandContext.cs,
tests/Contracts/WeatherContractTests.cs and test csproj reference, scoped
G1-03a proposal/handoff and this assignment. No module implementation, schema,
provider calls, host or domain threshold changes.

First compiled Weather ports/DTOs follow the accepted HLD and SQL time meanings.
The old GetKnownForecastAsync sketch omitted workspace and used internal UUID;
the freeze adds workspace and opaque public location ID without breaking wire.
CommandContext compiles the existing LLD caller-scope convention. Integration
Owner approves this proposal; no data owner, safety, budget or egress rule changes.

Write negative/positive contract tests before implementation. Verify unknown
issue time/value remains unknown; incompatible issue metadata fails; collection
copies protect immutable snapshots. Query selection and persistence are later
component tests, not behavior of these interfaces. All existing checks and a
separate QA review are required before merge. Gate 2 remains unauthorized.
