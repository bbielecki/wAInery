# G1-03b contract freeze handoff

Issue #20 / milestone #10; Architect / Integration Owner; BR-08/09/10; API cost 0.
Scope: new Vineyard, Winery, Observations Contracts; BuildingBlocks scoped command,
resolved identity and explicit confirmation value objects; contract tests/readmes.
No module implementation, migration or existing JSON schema/sample was changed.

Opaque IDs remain strings. Only owner lookup may produce ResolvedSubject with a
canonical UUID. Command scope is immutable and rejects wrong owner/workspace.
Batch and vessel are separate refs.
Vineyard owns optional opaque WeatherLocationId binding plus paged reverse lookup
for location→block impact mapping; no hardcoded Host or foreign schema mapping.
Location existence/ownership validation uses the Weather read seam (G1-03c).
Proposed narrative is not confirmation:
performed-operation commands require proposal ID/token/time and caller context.
Owner PrepareConfirmation issues the bound token only after explicit owner review;
it does not record performed work. Descriptive operation amount/material remains
optional and unknown when missing, without guessing an addition or intervention.
Token authenticity, permissions and target/time binding are owner responsibilities.

As-of DTOs include knownAt and applicable observed/received/recorded times. Arrays
are immutable. Observation value is a closed numeric/narrative/bool/categorical
sum, with explicit numeric unit; zero/false are not null. Intake quality differs
from stored VALID/SUSPECT. Quarantine must be rejected by the future owner, never
inserted canonically. Pages use copied type filters, max1..500 and opaque cursor.

RED: CS0234 before public types existed. Actual local verification:

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Build zero warnings/errors; architecture22; 59 PASS (38 contracts,21 actual PG),
zero failed/skipped. Historical five schemas/two negatives and skills PASS.
New tests exercise real constructors/scope/confirmation/value/filter behavior.
Synthetic numeric v1 sample is manually consumed with JsonDocument into the
in-process DTO, preserving opaque IDs and decimal. This is not an implemented
HTTP decoder or proof of database resolution/as-of behavior. No fake ports used.
Tests for actual unknown-ID lookup, as-of storage, authorization/token validation,
unit conversions and owner atomic commands remain later Gates; no claim of pass.

Independent /root/qa_g1_01 found missing block/weather mapping and confirmation
token issuance seams; both now explicit owner ports. Final rereview: no open
P0/P1/P2. Reviewer checked Contracts-only imports, unchanged wire/migrations and
whitespace, and did not run .NET/PG. Final fresh-main tests/CI required before merge.
No private data, protected excerpts, live providers or paid APIs. No database
changes/outbox risk from this contract-only PR. Consumers may implement only after
merge; Connectivity and Decisions owners must acknowledge this handoff when
assigned. Relevant prior verified scope/opaque-ID lessons reused; no new
transferable finding or skill candidate claimed. Gate 1 still incomplete.
