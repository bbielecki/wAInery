# G1-05 handoff — real PostgreSQL migration foundation

Ticket #16, milestone #10; BR-09/10; Integration Owner; application API cost 0.
Shared Persistence is migration infrastructure, not a new bounded context/service.
No module implementations or source/economic policies changed. Secrets remain
in environment variables, never output or committed. All test data is synthetic.

## Behavior and ownership

Embedded 001 materializes the reviewed blueprint; its SQL body is identical to
the reference apart from comments. Runtime migrations are now canonical. A
PostgreSQL transaction advisory lock serializes migrators; the same transaction
applies pending DDL and SHA256 journal entries. Checksums normalize CRLF, and
version/name/payload drift fails closed. All SQL commands have explicit 60-second
limits and cancellation. No destructive reset or retry loop is implemented.

Host --migrate requires secret WAINERY_DB_CONNECTION, reports applied count and
returns. Repeated run applies zero. Missing/invalid configuration exits 1 without
echoing values; normal Host mode remains scaffold health, not decision readiness.
No real developer DB was used in verification. Testcontainers creates unique
databases in its own pgvector:pg17 container, binds localhost, and disposes it.

## Actual tests and independent review

RED: new integration tests failed CS0234 before MigrationRunner existed. Initial
build also caught xUnit1051; cancellation was propagated, not suppressed.
Executed on SDK 10.0.100 / Windows (task-specific temporary DOTNET_CLI_HOME):

```powershell
dotnet restore wAInery.slnx --verbosity quiet
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Latest full solution: 41 PASS, 0 failed/skipped (32 contract, 9 integration).
Release build: zero warnings/errors. Architecture: 22 valid projects, no cycles
or foreign implementation references. Five historical schemas/two negatives,
skills and whitespace PASS. Real PostgreSQL cases prove:

- vector functions and all ten schemas exist after migration;
- second execution is a no-op, two concurrent migrators apply once;
- modified payload and unknown future journal version are rejected;
- failing next DDL and journal entry roll back together;
- two definition versions coexist while one-active uniqueness is enforced;
- real Host CLI returns applied 1 then 0, rejects missing config and sanitizes
  malformed config; stdout/stderr contain neither connection string nor password.

Independent /root/qa_g1_01 checked runtime/reference SQL equality, 22-project
graph, transaction/history logic, CLI, isolation/cleanup and diff. Found P2:
history read/journal insert inherited a potentially unbounded connection timeout;
both now explicitly use 60 seconds. Fixture also receives the test cancellation
token. Re-review: no open P0/P1/P2; READY FOR PR subject to final fresh-main CI.
Reviewer did not claim running the implementer's PostgreSQL suite. Full 41-test
rerun passed after the review changes.

## Use, risk and remaining work

Configure WAINERY_DB_CONNECTION privately, then run:

```powershell
dotnet run --project src/Host/wAInery.Host.csproj -- --migrate
```

For the reproducible synthetic demonstration, run the integration suite above:
it exercises the actual Host against its own disposable database. Expected CLI
counts are 1 then 0; malformed or absent configuration returns 1 without secrets.
Migration incompatibility requires a new reviewed additive version, never editing
a published checksum. Test migration rollback is not a backup/restore drill.

Scope does not yet include occupancy overlap, same-workspace correction or used
definition payload immutability (G1-06); accepted canonical writes/outbox, replay
effects/jobs are G1-07/08. Remaining public port seams are also pending. Actual
decision quality, RAG and calibrated predictions have not been evaluated.

Transferable finding candidate: explicitly bound every migration journal command
instead of trusting caller Command Timeout; verify no-op/concurrency/checksum and
failure rollback on real PostgreSQL. Evidence is the executed suite and review.
No new skill: existing wainery-pr-verification covers the repeated review workflow.
No measured savings claimed. Receiving module owners use this shared foundation
only after merge; this notice does not assert their acknowledgement.
Gate 1 remains incomplete and Gate 2 needs APPROVE GATE 1.
