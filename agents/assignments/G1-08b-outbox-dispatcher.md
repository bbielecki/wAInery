# G1-08b transactional delivery work order

Issue28 / Gate1 #10; IO; BR-09/10; API0. PR26/29/31 merged; one shared writer.
Stage Independently reviewed, final fresh-main regression/CI pending.
Allowlist Persistence/PostgresOutboxDispatcher.cs, additive004_outbox_delivery.sql,
Integration/OutboxDispatcherTests.cs from C separate worktree, scoped handoff.
No owner/Decisions implementation, broker, runtime swarm or external effect.

API frozen issue28 before C compile RED: IOutboxConsumer(Name,HandleAsync using
supplied Npgsql connection/transaction/DomainChangeV1/ct), PostgresOutboxDispatcher
with copied unique registrations and DispatchOneAsync. Standard Npgsql tx/savepoint,
existing strict codec and shared jobs, no new framework/package.

Row lock precedes savepoint; decode validates row identity/scope/type/version.
All consumers' inbox/effects/ack commit together. Rollback to savepoint on failure
retains row lock, clears partial effects and commits fixed retry/DEAD metadata.
Cancellation propagates with whole transaction rollback. DB-clock backoff cap300s,
attempt limit1..20. Handler only short DB work, no model/network/physical effects.
Current source cannot silently drain events with zero consumers. New subscribers
and historical backfill need an explicit future owner work order.

C authored22 real DB probes; initial compile RED before IO source, runtime not
claimed by author. Root integrates/runs suites; another session reviews whole diff.
Published001/002/003 unchanged. Fresh-main final tests/CI before squash.
Gate1 incomplete; Gate2 requires APPROVE GATE 1.
