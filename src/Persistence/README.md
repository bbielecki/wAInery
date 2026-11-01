# Shared migration infrastructure

Integration Owner owns this assembly and embedded, versioned SQL. It is not a
bounded context or service. Canonical runtime DDL lives in Migrations; docs SQL
is a historical design reference. Never edit a published migration: add the next
version and verify it against real PostgreSQL/pgvector.

MigrationRunner serializes migrators with a transaction advisory lock, verifies
the compiled catalog against a SHA256 journal (line endings normalized), and
commits pending DDL plus journal atomically. History drift fails closed. Commands
are cancellable and bounded; no retry loop or destructive reset exists.

After configuring the secret WAINERY_DB_CONNECTION locally, run:

```powershell
dotnet run --project src/Host/wAInery.Host.csproj -- --migrate
```

First run reports applied migration count; repeated run reports zero. The normal
Host still exposes scaffold health only; this command does not run decision
workers. Never log/commit connection strings or use real books/data in tests.
Fixture-owned Testcontainers databases are independent of a developer's local DB.
