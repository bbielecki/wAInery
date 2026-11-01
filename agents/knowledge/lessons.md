# Verified engineering lessons

Gate1 / IO + independent QA /2026-11-01. Synthetic tests, not business/scientific validation.

- **PG precision / BR-08/09:** normalize persisted timestamps before JSON; keep source TimeRange precision.
  [PR26](https://github.com/bbielecki/wAInery/pull/26), [OutboxAtomicityTests](../../tests/Integration/OutboxAtomicityTests.cs):
  `Event_timestamp_columns_and_payload_share_PostgreSQL_microsecond_precision`.
- **Wrong database / BR-09/10:** valid connection+transaction can still target another DB; check configured host/port/database, never secrets/DNS guesses.
  [PR29](https://github.com/bbielecki/wAInery/pull/29), [JobQueueTests](../../tests/Integration/JobQueueTests.cs):
  `Caller_transaction_must_target_the_queues_configured_database` (two actual DBs).
- **Cancellation / BR-09/10:** consumer-local OCE with live caller must consume bounded failure budget; caller cancellation rolls back without retry metadata. DB-only consumers; no exactly-once external I/O claim.
  [PR33](https://github.com/bbielecki/wAInery/pull/33), [OutboxDispatcherTests](../../tests/Integration/OutboxDispatcherTests.cs):
  `Consumer_local_cancellation_with_live_caller_is_a_bounded_failure`, `Cancellation_after_a_handler_write_rolls_back_and_propagates_without_retry_metadata`.
- **Reload / BR-10:** one-time endpoint validation is bypassable if Kestrel reload stays enabled. Disable endpoint reload, retain startup guard; other settings may still reload.
  [PR37](https://github.com/bbielecki/wAInery/pull/37), [HostReadinessTests](../../tests/Integration/HostReadinessTests.cs):
  `Changed_endpoint_file_after_startup_cannot_add_a_listener` (actual second-loopback RED?GREEN).

Other findings stay in PR handoffs/tests. B/C received relevant interface lessons;
ACK and reused skills: [Gate1](../handoffs/GATE-1-review.md). No new skill/savings claim.

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
