# G1-07 atomic outbox handoff

Issue #24 / Gate1 #10; Architect / Integration Owner; BR-09/10; API cost0.
IO owns OutboxWriter/Persistence project reference; separate QA session authored
the real PostgreSQL cases in an isolated worktree. Stream C performs independent
review of both helper and tests. No module implementation or migration change.

## Behavior and limits

Append requires an open caller connection and its active transaction, matching
workspace/canonical UUID and canonical event producer. It never opens, commits or
rolls back caller transactions. All values are parameters, timeout30 and caller
cancellation apply. Strict v1 codec snapshots fields; full safe wire envelope is
stored alongside metadata. SQL aggregate UUID remains separate from opaque wire
IDs. Logical version/event conflict aborts the transaction; it is never ignored.

Three physical event timestamps quantize to UTC whole microseconds before both
payload/columns. .NET extreme date sentinels, including values rounded to minimum,
are rejected instead of implicit PostgreSQL infinity. Payload-only TimeRange and
source data retain their precision. Owners must normalize their canonical times
too and fail/rollback the entire command on append error. The helper cannot prove
future owners obey the boundary or implement command/source idempotency.

## Actual evidence

QA test-first compile RED: missing BB transitive reference and OutboxMetadata/
writer types before implementation. Initial combined 34 PG cases PASS. New real
timestamp case then failed (ticks17 vs expected10); two extreme dates also wrongly
passed. RED37 cases =34 PASS/3FAIL. After fix, full solution75 PASS, zero failures/
skips (38 contract+37 PostgreSQL); Release0 warnings/errors; architecture22.
Five historical JSON fixtures/two negatives, skills and whitespace PASS.

```powershell
dotnet restore wAInery.slnx --verbosity quiet
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

16 actual outbox cases cover both-row visibility only after caller commit,
rollback before/after append, disposal without commit, missing/foreign/completed
transaction, workspace/UUID/producer guards, duplicate logical-version rollback,
strict complete payload roundtrip/parameterization and timestamp precision.
Only synthetic fixture aggregate in fixture-owned disposable databases. No fake
Vineyard/Winery handler or claim of their as-of/confirmation/business correctness.
Disposal/rollback is not evidence of an OS process-kill or backup/restore drill.

QA authorship: /root/qa_g1_01 test commits c004292 and458d5c7 (integrated e113def,
f9bef5c); IO added the two extreme timestamp regressions. Reviewer is separate
/root/stream_c_knowledge, not the test author. Source/static checks matched15/15
producer events to codec, only BB shared dependency, no own transaction lifetime,
explicit timeout/cancellation, unchanged migrations/wire/Host. Final precision
rereview found no open P0/P1/P2, READY FOR PR. Final fresh-main tests/CI must pass
before squash; do not claim reviewer ran PG.

## Demo, risk and learning

Run the integration command above for reproducible synthetic transaction proof.
Canonical and outbox counts are0 before commit, both1 after commit, both0 after
rollback; logical duplicate retains original canonical value/event only once.
Dispatcher/inbox/jobs/replay are G1-08, not provided by this append helper.
No provider, live weather, protected data, private prompt or paid API was used.
Existing connection secrets remain private and no SQL values are logged.

Verified lesson candidate: .NET ticks and PostgreSQL microseconds differ; quantize
before serializing the event rather than trusting implicit driver truncation.
Evidence is the reproduced PG failure, corrected helper and regression. Primary
references: https://www.npgsql.org/doc/types/datetime.html and
https://www.npgsql.org/doc/basic-usage.html#transactions.
Consumers: all canonical owners; receiving acknowledgements needed when assigned.
Existing PR/contract skills reused; no new skill proposed or measured savings.
Gate 1 remains incomplete; Gate 2 requires APPROVE GATE 1.
