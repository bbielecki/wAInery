# G1-02 handoff — compiled common event boundary

Ticket #12; milestone #10; BR-01/08/09. Integration Owner.
Scope: common DomainChangeV1/scoped-to-wire codec and executable contract suite.
No module implementation, SQL/migration/outbox, rights or economic policy change.
Five wire v1 schemas are unchanged. Consumers may use this event only after merge;
the remaining module ports are still pending and Wave 0 is not complete.

## Design and changed surfaces

- BuildingBlocks owns the common event and codec. IDs remain opaque strings,
  with workspace/correlation/event UUIDs where the existing v1 requires them.
- Explicit mapping preserves all required wire fields and optional safe source
  metadata/time window. In-process SubjectRef includes matching workspace.
- Null is forbidden for every present v1 event field; optional CLR null means
  omission on output. Unknown properties are rejected. RFC3339 timestamps need
  seconds and explicit offset; valid lowercase t/z and offsets normalize to UTC.
- Tests use xUnit v3 / native Microsoft.Testing.Platform on SDK 10, with pinned
  test-only JsonSchema.Net to validate emitted JSON against the existing schema.
- ArchitectureChecks permits extra test projects while requiring all eight
  module pairs. Module Contracts guards apply to the whole directory;
  tests/Contracts is not misclassified as a module's public assembly.
- CI now executes the .NET contract suite and five historical Python schemas.
- Surfaces: src/BuildingBlocks, tests/Contracts, tests/Architecture/Program.cs,
  Directory.Packages.props, global.json, solution, existing monorepo CI,
  assignment/proposal and this handoff. No runtime package dependencies added.

## Test-first and executed proof

Initial consumer test RED: CS0103, absent DomainChangeV1Json. During independent
review, five explicit-null cases failed before the fix, and three timezone/case
cases failed before the RFC3339 converter. All these regressions now pass.
Runner setup initially exposed the unsupported VSTest/MTP combination on .NET 10;
global.json now selects MTP and commands use --project/--solution as required.

Executed locally (SDK 10.0.100 / Windows):

```powershell
dotnet restore wAInery.slnx --verbosity quiet
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Latest contract result: 26 passed, 0 failed/skipped. Build: zero warnings/errors.
Architecture: 20 projects PASS. Historical schemas and two negative fixtures,
skills validation and whitespace checks PASS. DOTNET_CLI_HOME uses a task-specific
temporary directory; user/global configuration remains unchanged.

Additional real architecture negative check: Python stdlib copied project XML,
solution and ArchitectureChecks binaries to a validated unique temporary root,
added Weather/Contracts/Extra.csproj importing Weather implementation, and ran
the copied executable. It rejected the reference with the expected diagnostic.
Windows child error mode suppressed an OS crash dialog for this deliberately
failing console process; the initial attempt timed out, the bounded retry passed.
Temporary fixture was removed; no foreign module files were edited.

## Independent review and limits

Native QA session /root/qa_g1_01 reviewed source/tests and compared all three enum
sets to frozen schema. Its own Python checks confirmed explicit-null/offset-free
invalid cases and offset/lowercase valid cases. Three findings were corrected:
null acceptance, timestamp ambiguity and Contracts guard based on filename.
Final verdict: no open P0/P1/P2, READY FOR PR subject to latest-main CI.
Reviewer did not claim to execute the implementer's .NET build/test commands.

No database tests executed: migrations/outbox do not exist yet. No agronomic
behavior or numerical prediction quality claimed. Application API cost 0;
all fixtures synthetic; no books, raw operational data, prompts or credentials.

Transferable finding: optional wire fields need absence/null tests independent
of CLR nullability; ISO parsing alone can also admit timestamps without offset.
Proof is in the named negative tests and reviewer replay above. Consumers are
all event publishers and Decisions; this handoff is their frozen-boundary notice.
Upstream source-glob lesson is recorded in the [G1-01 handoff](G1-01-reproducible-tools.md)
and [PR #11](https://github.com/bbielecki/wAInery/pull/11). No new skill proposed
without repeated stable workflow evidence; no measured savings claimed.

Skills reused: wainery-contract-change and wainery-pr-verification.
Next: module read/command DTO seams and controlled reasoning/read contract,
then actual PostgreSQL migration and durable transaction work. Gate 2 requires
APPROVE GATE 1 and remains unauthorized.
