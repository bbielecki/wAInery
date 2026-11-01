# G1-07 atomic write work order

Issue #24 / milestone #10; Architect / Integration Owner; BR-09/10; API cost 0.
Stage Independently reviewed, final fresh-main tests/CI pending.
Branch agent/integration/g1-07-atomic-outbox. User requested real
parallel workstreams; [canonical topology](../../docs/00-governance/agent-team-topology.md)
still controls ownership and the Wave 0 gate. B (#22) and C (#23) independently
prepare module proposals in isolated worktrees; shared implementation is IO-only.

IO allowlist: src/Persistence/OutboxWriter.cs, Persistence csproj (BB reference),
this work order and scoped handoff. QA author /root/qa_g1_01 owns ONLY new
tests/Integration/OutboxAtomicityTests.cs in agent/quality/g1-07-outbox-tests.
Its test commit is integrated after the IO API below; another session must review
the entire final PR. No module owner implementation or migration change.

## Approved infrastructure seam

OutboxMetadata(WorkspaceId Workspace, string Producer, string AggregateKind,
Guid InternalAggregateId); OutboxWriter.AppendAsync(NpgsqlConnection connection,
NpgsqlTransaction transaction, OutboxMetadata metadata, DomainChangeV1 change,
CancellationToken ct) returns Task. Caller owns an open connection and active
matching transaction. Helper never opens/commits/rolls back caller transaction.
Canonical identity and metadata workspace must be valid; event metadata uses the
strict existing v1 codec. Canonical event producer must match its owning context.
Store full safe wire envelope as jsonb, separate internal aggregate UUID column
from wire opaque aggregate/subject IDs. Parameters, explicit timeout/cancellation.
No ON CONFLICT ignore; duplicate logical events abort transaction. Owner must
normalize its canonical timestamps to database precision too: persisted event
timestamps quantize to UTC whole microseconds before payload/columns; .NET extreme
date sentinels are rejected instead of Npgsql infinity. Source TimeRange unaffected.
Owner must
rollback the entire accepted write on any append error, validate source identity,
and implement canonical idempotency. This helper cannot prove future owner usage.

## Verification and handoff

QA writes failing compile case first and reports RED before IO implementation.
Actual disposable PostgreSQL tests prove commit visibility/rollback/strict wire
roundtrip/duplicate uniqueness and caller transaction guard. Synthetic aggregate
only, never private vineyard or copyrighted content. Full solution regression,
architecture, schema/skill checks, independent review and updated CI follow each
integration; fix failures before squash, rebase onto latest main first.
No transferable finding assumed. Gate 1 incomplete; Gate 2 unauthorized.
