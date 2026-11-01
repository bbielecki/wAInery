# AIExecution/Contracts

Compiled in-process contracts in `UsageContracts.cs`, IO freeze G1-03c (#27).
These are public DTO/port declarations, not implemented handlers or direct JSONv1
serialization models. See the scoped IO work order and canonical component design.
Actual current-rights checks, storage, lookup, as-of/pagination and provider usage
require owner implementation tests in later approved Gates. No new permission is
granted by constructing a metadata DTO. Ordinary CI uses synthetic data only.

Public module-owned request/response DTOs and query/command interfaces. Cross-module change proposals must be approved by integration owner before freezing.

See `docs/30-components/AIExecution.md` and the agent assignment.
