# Weather/Contracts

First compiled in-process Weather contracts (G1-03a, Gate 1). No provider,
persistence, forecast-selection implementation or HTTP API is included.

Weather owns forecast runs and semantic diffs; Decisions owns risk/materiality.
Read queries carry workspace, opaque location ID, as-of and valid window.
Result errors distinguish unknown location from a known location with MISSING
forecast. MissingWindows describes coverage separately from forecast freshness.
IssuedAt stays null with Unknown basis when the provider does not supply it;
nullable point Value means unavailable, while zero remains an explicit forecast value.

Runs defensively copy points and normalize timestamps to UTC. Source identity,
version, variable code and units travel with the immutable data. Canonical writes
are commands with validated caller scope/idempotency and require an owning
application implementation plus atomic outbox in later work.

The exported C# types supersede the old unscoped Guid-location sketch. Existing
wire schemas are unchanged. See agents/handoffs/G1-03a-contract-change.md and
docs/30-components/Weather.md; consumers implement only after contract merge.
