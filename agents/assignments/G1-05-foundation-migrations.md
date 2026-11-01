# G1-05 migration foundation work order

Ticket #16, milestone #10; BR-09/10; Integration Owner; API budget 0.
Stage: Review; branch agent/integration/g1-05-foundation-migrations.
Upstream #11/#13 merged; other owner DTO seams are independent and still pending.

Allowlist: src/Persistence/** (shared migration assembly, infrastructure only),
Host Program/csproj for --migrate, tests/Integration/**, Directory.Packages.props,
solution, existing monorepo CI test name/timeout, docs/50-data/sql/001_foundation.sql
reference pointer, this assignment and scoped migration handoff. No module
implementation, user/global config, private records or destructive user DB work.

Design: embedded versioned SQL copied from approved blueprint; PostgreSQL
transaction advisory lock, same-transaction journal with normalized SHA256,
history/checksum validation and bounded cancellable commands. No new service/BC.
CLI connection string is secret env WAINERY_DB_CONNECTION and never logged.

Test-first real Testcontainers pgvector/Postgres: fresh/repeat/concurrent
migrators, changed checksum/future version, transactional failed DDL and composite
prediction version/active-index behavior. Databases are unique and ephemeral
inside the fixture-owned container; no existing local DB is used.
G1-06 integrity constraints and G1-07/08 outbox behavior are explicitly separate.
Full local suites, independent QA, fresh-main rebase and CI before squash.
Wave 0/Gate 1 is incomplete; Gate 2 remains unauthorized.
