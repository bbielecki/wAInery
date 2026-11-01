PostgreSQL-backed tests, ingestion flows, idempotency, durable jobs and recovery.
## Executable foundation suite

Requires .NET 10 and a local Linux-container Docker daemon. Uses unique synthetic
databases inside a fixture-owned PostgreSQL/pgvector container, never an existing
developer database. Published test ports bind to 127.0.0.1; fixture disposal removes
its container. No book, model or weather endpoint is needed.

```powershell
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release
```

Cases cover real foundation migration, repeat/concurrent migrators, checksum and
future-version rejection, DDL/journal rollback, definition versions/active index,
and the Host --migrate command including sanitized failure paths. Remaining
G1-06 constraints and G1-07/08 outbox behavior are not implied by these tests.
