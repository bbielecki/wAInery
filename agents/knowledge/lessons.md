# Verified engineering lessons

Gate1 verified engineering lessons, curated by IO on2026-11-01. Evidence below is
synthetic contract/actual PostgreSQL/process regression, not RAG quality, agronomic
validity or production behavior. Architecture/contracts remain authoritative.

### LES-2026-001 — Nullable does not define omitted JSON

Verified / BB, all event owners / BR-08/09. Explicit null failed frozenv1 even
though CLR field was nullable. Preserve omission vsnull deliberately at codec
boundary. [PR13](https://github.com/bbielecki/wAInery/pull/13),
[DomainChangeV1Tests](../../tests/Contracts/DomainChangeV1Tests.cs), exact regression
`Optional_fields_cannot_be_explicit_null_in_v1`. Limit: other schemas may allow null.
B DataAcquisition ACK; no new skill, schema/codec tests enforce this.

### LES-2026-002 — Persisted timestamps need shared precision

Verified / Persistence, event owners / BR-08/09. .NET100ns event values and PG
microseconds diverged between JSON and columns. Normalize the three persisted
timestamps before serialization; do not truncate arbitrary source TimeRange.
[PR26](https://github.com/bbielecki/wAInery/pull/26),
[OutboxAtomicityTests](../../tests/Integration/OutboxAtomicityTests.cs), exact
`Event_timestamp_columns_and_payload_share_PostgreSQL_microsecond_precision`.
B ACK; actual PG regression, no performance/savings claim or new skill.

### LES-2026-003 — A valid transaction can target the wrong database

Verified / Persistence, future job publishers / BR-09/10. Connection+transaction
matching alone accepted a foreign DB. Check actual configured host/port/database
before queue publication, without logging/comparing secrets. Deliberately no DNS
alias equivalence guess. [PR29](https://github.com/bbielecki/wAInery/pull/29),
[JobQueueTests](../../tests/Integration/JobQueueTests.cs), exact regression
`Caller_transaction_must_target_the_queues_configured_database` uses two real DBs.
B ACK; replay/lease handoff remains canonical. No new skill.

### LES-2026-004 — Local OCE is not caller cancellation

Verified / Persistence, DB-only outbox consumers / BR-09/10. Consumer-local OCE
with live caller bypassed bounded retries. Roll back effects and record bounded
failure unless caller actually cancelled. Caller cancellation propagates full
rollback with no retry metadata. [PR33](https://github.com/bbielecki/wAInery/pull/33),
[OutboxDispatcherTests](../../tests/Integration/OutboxDispatcherTests.cs), exact
`Consumer_local_cancellation_with_live_caller_is_a_bounded_failure` and
`Cancellation_after_a_handler_write_rolls_back_and_propagates_without_retry_metadata`.
B ACK; this is not exactly-once external I/O. Deterministic tests, no new skill.

### LES-2026-005 — Startup validation can be bypassed by reload

Verified / Host / BR-10. Startup rejected endpoint overrides, but subsequent JSON
reload added an unapproved listener. Disable Kestrel endpoint reload as well as
validate initial config. [PR37](https://github.com/bbielecki/wAInery/pull/37),
[HostReadinessTests](../../tests/Integration/HostReadinessTests.cs), exact regression
`Changed_endpoint_file_after_startup_cannot_add_a_listener`: actual RED with second
loopback HTTP listener, then GREEN after source fix. No public bind during RED.
Limit: this is endpoint config, not a ban on every application setting reload.
QA independently identified/reviewed, IO accepted. Test beats new instruction skill.

## Entry template

### LES-YYYY-NNN — Short, searchable problem title

- **Status:** Candidate / Verified / Superseded
- **Affected modules and business requirement:** e.g. Weather, Decisions / BR-xx
- **Problem and trigger:** what actually failed or was unexpectedly costly
- **Reproduction:** synthetic fixture, command or failing test (no user/private data)
- **Root cause (supported evidence):** links to GitHub PR/commit/test output
- **Resolution:** concise algorithm or change and its limits
- **Regression proof:** positive and negative test IDs; CI link
- **Consumers informed:** modules/agents/PR handoffs
- **Consider skill?** repeated frequency, expected savings, proposed owner
- **Superseded by:** ADR, canonical contract or newer lesson if applicable

Don't record unfounded speculation, chat transcripts, raw prompts, copyrighted book passages, external model contexts, secrets, or task progress already captured in GitHub. If a correction changes a design invariant, update owner documentation/ADR instead of letting this page become a competing source of truth.
