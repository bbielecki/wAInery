# G1-03a Weather contracts handoff

Ticket #14, milestone #10; BR-01/06/09. Integration Owner implements the
Weather-owned exported seam. No module implementation, SQL or wire changes.
All scopes, decisions and allowlists are in the assignment and contract proposal.

Compiled read/command ports include workspace and opaque IDs; Result<T>
distinguishes NOT_FOUND from MISSING. Time window coverage is separate from age.
Forecast runs preserve nullable provider issue time and numeric values, enforce
issue-basis consistency, copy points defensively and normalize times to UTC.
CommandContext compiles the existing caller-scope/idempotency convention and
does not imply approval of a physical operation. Publication rejects a mismatch
between command and run workspace. Read/write implementation remains future work.

Executed on SDK 10.0.100 / Windows (task-specific temporary DOTNET_CLI_HOME):

```powershell
dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

RED was CS0234 because no Weather contract types existed. Latest GREEN:
32 tests PASS, 0 failed/skipped; build 0 warnings/errors; architecture 20 projects
PASS; five historical wire fixtures + two negatives, skills and diff check PASS.
No query implementation, actual migration or PostgreSQL behavior was tested.

Independent /root/qa_g1_01 inspected all source, tests, proposal and README;
own whitespace check PASS. Found stale Contracts README; corrected before freeze.
Final review: no open P0/P1/P2, READY FOR PR conditional on fresh-main and CI.
The reviewer did not claim execution of implementer .NET tests.

No private data, secrets, model/weather calls or new paid dependency; API cost 0.
Receiving owners: Connectivity, Vineyard and Decisions; consumers implement only
after merge/rebase and this notice does not claim their acknowledgement.
No transferable finding beyond established scope/time conventions; no new skill.
Reused wainery-contract-change and wainery-pr-verification. Remaining Gate 1:
other owner seams, reasoning/read, real migrations/constraints, outbox and recovery.
Gate 2 remains blocked until APPROVE GATE 1.
