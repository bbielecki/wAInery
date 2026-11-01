# G1-06 integrity work order

Ticket #18 / milestone #10; IO; BR-08/09/10; API budget 0.
Stage Verified / independently reviewed; final fresh-main CI pending.
Branch agent/integration/g1-06-integrity-constraints.
Allowlist: additive migration 002_integrity.sql, IntegrityTests.cs,
MigrationTests.cs (catalog count), this assignment and scoped handoff.
Published migration 001 must remain byte-for-byte unchanged.

Enforce accepted owner invariants: vessel occupancy half-open intervals must not
overlap; correction references stay within workspace and cannot self-reference;
persisted versioned prediction payload is immutable, active selection remains
mutable. No draft-state exists in the table, so changed payload publishes a new
version, including before use. No scientific threshold or ownership change.

Test-first actual Postgres constraints with positive boundary cases. All data
is synthetic and fixture-owned; no production command or owner schema writes
outside a reviewed migration are introduced. Do not auto-repair/delete invalid
existing rows. Actual owner commands/as-of and replay remain further work.
Independent review and fresh-main full suites/CI required before squash.
Gate 2 stays unauthorized.
