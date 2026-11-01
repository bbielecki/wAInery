# G1-08a durable jobs work order

Issue #25 / Gate 1 #10; IO; BR-09/10; API0. Upstream PR26 merged69780c7.
Stage Independently reviewed; final fresh-main regression/CI pending.
Allowlist: Persistence/PostgresJobQueue.cs, additive003_job_leases.sql, scoped
assignment/handoff; QA author B owns Integration/JobQueueTests.cs. No module code.
IntegrityTests upgrade count follows the growing catalog; existing invariant
assertions remain intact. Fix xUnit2031 in imported QA test before runtime checks.
IO API and initial semantics approved in issue25 before test-first compile RED.

Unique workspace/kind/scope with monotonic nonnegative desired revision. Old/same
revision cannot revive terminal jobs. New input resets terminal attempt budget,
priority/time; pending coalesces earliest NotBefore/lower numeric priority. Running
lease stays active while desired revision advances. Technical retry2^attempt sec,
max300s; attempts1..20; lease positive<=15min. No paid model execution here.

Claim uses SKIP LOCKED, unique token and persisted claimed_revision. Completion
requires active caller transaction, matching scope/token/revision and live lease;
supports same-transaction synthetic publication.
Caller transaction endpoint/database must match the configured queue data source;
matching only its own connection/transaction pair is insufficient. Identity checks
never emit connection values and do not guess DNS aliases.
New desired input returns false and requeues/reset budget; expired attempts
recover or reach DEAD. Current time is DB
time; check lease after acquiring lock. Safe enum-only error codes. Published001/
002 unchanged; no silent repair of invalid legacy lease data.

B authored20 actual DB cases in isolated worktree; initial compile RED verified
before source existed. Root integrates, runs PG/full suites and corrects failures.
Independent final reviewer must be another session. Synthetic queue/publication
proof is not actual Decisions CAS or an OS process-kill. Gate2 unauthorized.
