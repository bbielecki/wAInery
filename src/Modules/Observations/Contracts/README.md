# Observations/Contracts

Compiled in-process contracts in `ObservationContracts.cs`, frozen by IO work order G1-03b (#20).

Public module-owned request/response DTOs and query/command interfaces. Cross-module change proposals must be approved by integration owner before freezing.

See `docs/30-components/Observations.md` and `agents/assignments/G1-03b-owner-contracts.md`.
These DTOs are not direct JSON v1 serialization models or implemented handlers.
Actual scoped lookup, as-of filtering, authorization, unit/registry validation and
confirmation-token verification require owner implementation tests in later gates.
Cancellation and NOT_FOUND/VALIDATION/CONFLICT Result errors are part of the seam.
