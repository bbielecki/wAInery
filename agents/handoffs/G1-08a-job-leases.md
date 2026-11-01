# G1-08a durable job lease handoff

Issue25 / Gate1 #10; Integration Owner; BR-09/10; API0. Shared queue/migration,
not Decisions implementation. Stream B authored initial20 actual DB cases in an
isolated worktree (4427bbd→aca3891); independent QA session is /root/qa_g1_01.
No module implementation, paid providers or private data. Published001/002 unchanged.

## Behavior

Unique workspace/kind/scope, monotonic desired revision, earliest pending schedule
and lower numeric priority. Same/old revision cannot revive terminal work. New
input resets terminal scheduling/attempt budget; running input changes retain
active lease. Claim uses atomic SKIP LOCKED and persistent random token/claimed
revision. New process with same worker name cannot complete an old token. Expired
leases recover; exhausted attempts reach DEAD. New revision never inherits an old
run's exhausted budget. DB-time retry2^attempt seconds capped300, attempt cap1..20,
lease positive<=15min. Failure codes are fixed enums, never exception text.

Enqueue/Complete require active caller transaction on the configured endpoint/DB;
same transaction permits a synthetic result write only when completion returns
true. Matching only connection/transaction was insufficient: review found wrong-DB
jobs could be accepted yet never claimed. Explicit host/port/database guard fixes
that without logging values or comparing secret credentials. Different DB roles
on the same configured target remain possible; DNS aliases are not guessed.
Completion locks first and checks expiry afterwards. New desired revision returns
false, clears old lease, resets budget and requeues; stale/expired token cannot
publish or mutate current work. Real prediction input/state CAS remains Gate2.

## Actual verification and limitations

Before source, B executed compile RED for missing queue types. First integration
build found xUnit2031 in imported test, fixed with supported Assert.Single overload,
without suppression. Existing upgrade assertion now counts all pending catalog
versions; invariant assertions unchanged. Initial57PG and full95 tests PASS.
Review's two wrong-DB tests then reproduced RED57PASS/2FAIL; after target guard,
59PG PASS. Two expiry-during-row-lock cases then passed too:61PG PASS. They prove
lock wait while the lease is live using pg_stat_activity/pg_blocking_pids, wait by
DB clock, unblock without tuple UPDATE (no EPQ masking), then reject completion
and failure without publication. Cleanup is bounded and observes the worker task.
Final full99 rerun follows current-main rebase.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Build0 warnings/errors; architecture22; historical JSON5 examples/2 negatives,
skills, unchanged published migrations and whitespace PASS. Reviewer independently
inspects source/diff and does not claim implementer's PostgreSQL runs. No actual
Decisions pipeline, LLM, live source, process-kill or backup/restore test executed.
Synthetic publication proves only same-transaction queue completion fencing.
Final independent rereview: no open P0/P1/P2, READY FOR PR. Review closed the
configured-DB P1 and expiry-wait coverage P2. Current-main tests and green CI
required before squash; reviewer did not execute PostgreSQL.

## Demo, risk and learning

Run the integration command above. Expected: concurrent claims choose distinct
scopes, locked row is skipped, old token/revision returns false, latest revision
is requeued with attempt1, bounded failure becomes DEAD, rollback preserves both
RUNNING state and absent synthetic publication. DB-time manipulations affect only
fixture-owned databases, never developer/user data. New003 intentionally rejects
invalid legacy lease state rather than silently manufacturing fencing tokens.

Transferable candidate: a valid connection/transaction pair can still target the
wrong database; bind shared queue methods to their configured target. Proof is two
real DB failures and corrected regressions. Relevant receivers: all owner handlers
using shared jobs, future Decisions worker. No new skill: existing verification
workflow applies, measured benefit unknown. Gate1 incomplete; no Gate2 approval.
