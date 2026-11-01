# G1-08b transactional dispatcher handoff

Issue28 / Gate1 #10; IO; BR-09/10; API0. C authored22 database probes in an
isolated worktree (887c1a3→027ea25); IO wrote dispatcher after compile RED.
Standard Npgsql transactions/savepoints and existing strict event codec reused;
no broker, extra package, business/model consumer or physical action introduced.

One due pending row is locked SKIP LOCKED before a savepoint. Envelope identity,
workspace/type/aggregate/schema version must match row metadata. Registered
consumers are copied with unique stable names, at least one required. Each inbox
insert/effect and final delivered acknowledgement commits in the same transaction.
Replayed inbox skips the effect. Error rollback-to-savepoint removes all partial
effects/inboxes while retaining outer row lock; safe retry/DEAD metadata then
commits, preventing a rollback/reclaim race. SQL-aborted transactions recover at
savepoint too. Fixed error codes never include exception text or private payload.
Caller cancellation propagates and rolls back the whole attempt; a consumer's
own timeout/OCE with a live caller token is a bounded CONSUMER_FAILURE.
DB-time retry cap300s,
attempt limit1..20; already exhausted work marks DEAD without another handler.

Migration004 adds dead_at, terminal/attempt checks and pending index. Published
001/002/003 unchanged; no automatic data repair. Future subscriber registration/
backfill needs an explicit owner procedure; existing acknowledged events are not
automatically replayed for newly invented consumers. Runtime registration awaits
an actual Decisions consumer in Gate2; Host does not drain real events here.

## Actual evidence

Author compile RED missing IOutboxConsumer/dispatcher before source. Combined
actual PostgreSQL83 PASS,0 failed/skipped (22 dispatcher+61 prior), disposable
fixture databases only. Handler consumers are synthetic SQL transaction probes,
not fake DBs or claimed Decisions pipelines. Cases include forced duplicate,
SQL/C# failures then fresh-dispatcher recovery, all-or-nothing fanout, real shared
job enqueue/rollback in supplied tx, concurrent/locked-row skip, copied registry,
bounded retry/DEAD, malformed/mismatched envelopes, cancellation rollback and safe
error marker. Restart object proves durable continuation, not literal OS kill.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Review added exhausted-attempt and consumer-local cancellation cases. The latter
reproduced RED84PASS/1FAIL: every OCE had incorrectly bypassed retry. The filter now
checks the caller token, not exception type. Full137 PASS (52contract+85PG), zero
failed/skipped, after fixes. Independent /root/qa_g1_01 closed P1/OCE and P2/cap
coverage; final no open P0/P1/P2, READY FOR PR. Reviewer read source/static evidence,
did not run PostgreSQL. Fresh-main tests/CI still required before squash.
All data synthetic; no live weather/model/private books/prompts.
Only fixture-owned processes/DBs used, no real developer database reset.

## Limits and learning

Handlers must keep external I/O and model computation outside DB transactions.
Long-running or nontransactional effects are not made exactly-once by this API.
The helper proves transactional dispatch infrastructure, not scoped impact,
prediction CAS, scientific quality or real business confirmation correctness.
No measured performance/API savings claimed. Source-specific lesson candidate:
classify OCE using the runner's cancellation token; exception type alone can make
a poison consumer retry forever without accounting. Separate caller/local-token
regressions provide evidence.
Another source-specific candidate:
rollback effects to a savepoint acquired after the row lock so failure bookkeeping
cannot race another dispatcher. Actual C#/SQL failure/recovery cases are evidence.
Relevant receivers: Decisions/owner event consumers, acknowledged on assignment.
Existing verification skill reused; no new skill proposed. Gate1 incomplete,
Gate2 requires APPROVE GATE 1.
