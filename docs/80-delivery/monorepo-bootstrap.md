# Monorepo bootstrap — implementation layout (Wave 0 scaffold)

**Status:** file/project scaffold only. A successful build does not prove that shared read ports, actual Postgres migrations or business decision rules have been implemented. See [Business HLD](../10-architecture/business-hld.md), [Architecture Review](../70-quality/architecture-review-2026-11.md) and [contract freeze](../90-lld/implementation-contract-freeze.md).

## Structure

```text
wAInery.slnx
Directory.Build.props
Directory.Packages.props
global.json
src/
  BuildingBlocks/        # stable cross-module value objects; Integration Owner
  Host/                  # single localhost ASP.NET Core host and composition root
  Modules/
    Vineyard/
      Contracts/         # owner-published, immutable DTOs and public ports
      wAInery.Modules.Vineyard.csproj  # private domain/application/infrastructure code
    Winery/              # identical structure
    Decisions/
    Observations/
    Weather/
    Knowledge/
    Connectivity/
    AIExecution/
tests/
  Architecture/          # dependency graph smoke check, no external NuGet packages
  Contracts/             # future wire-schema tests
  Integration/           # future Testcontainers PostgreSQL migration and outbox tests
  Modules/               # module-owned unit/integration tests
  Evaluation/            # 30–50 reviewed business scenarios + replay/baselines
infra/local/compose.yaml # loopback-only PostgreSQL + pgvector
```

Each module has a **Contracts project** and a **single implementation project**. The implementation project may contain separate `Domain/`, `Application/` and `Infrastructure/` folders/namespaces internally without multiplying assemblies for the MVP. Splitting a module into physical assemblies later is an inexpensive refactor when it has concrete benefits.

## Local developer commands

```bash
dotnet restore wAInery.slnx
dotnet build wAInery.slnx --configuration Release
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
cp infra/local/.env.example infra/local/.env
docker compose --env-file infra/local/.env -f infra/local/compose.yaml up -d
dotnet run --project src/Host/wAInery.Host.csproj
# http://127.0.0.1:5100/health/live
```

**Prerequisites:** .NET 10 SDK and Docker/Podman with Compose. Replace the example PostgreSQL password before use. No weather provider or AI API key is needed for structural build. The Host exposes only a loopback health route. It does **not** connect to the database or run an agent yet.

## Ownership, dependency direction and contract freeze

- Integration Owner alone changes `wAInery.slnx`, `src/BuildingBlocks/**`, `src/Host/**`, root properties, CI, `contracts/**`, `infra/**`, initial migration assembly and shared test fixtures.
- Each module agent owns `src/Modules/<Module>/**` and `tests/Modules/<Module>/**` only.
- A module implementation can reference its own Contracts and external **Contracts**, not another module implementation. A module's Contracts assembly can reference BuildingBlocks, not other modules.
- Host is composition root; `Decisions` calls `AIExecution` behind the **Decisions-owned** reasoning contract; only Host wires it.
- SQL in `docs/50-data/sql/001_foundation.sql` is **reference design** and has not been applied. PostgreSQL schema owners submit migrations to Integration Owner; no concurrent mutation of shared DDL.
- Public subject/source identifiers remain opaque strings and map to internal UUIDs in owner modules. No leakage of EF entities between contexts.

## Remaining Wave 0 gates (do not report as complete)

1. Freeze and compile the actual public ports/DTOs from LLD; ensure source and JSON fixture compatibility.
2. Introduce migration runner and run initial migration against real PostgreSQL+pgvector via Testcontainers; test prediction-definition versions and occupancy exclusion constraints.
3. Implement canonical entity write + same-transaction outbox, inbox deduplication, restart/replay and workspace checks.
4. Build a **working** vertical weather → scoped invalidation → local plan report; record tokens/cost and verify zero AI on irrelevant changes.
5. Add reviewed real/synthetic evaluation fixtures and ownership/rights tests. A running skeleton does not meet the business acceptance gate.

## Why monorepo without Nx or a broker?

For a single local host and eight logical modules, .NET solution/project reference boundaries are sufficient. No Nx plugin, orchestrator service, Kubernetes, Kafka or RabbitMQ is necessary in MVP. GitHub Actions compiles the solution and the architecture check scans project references. Later CI can use path-based test selection and cached restore after the test suite creates a measured bottleneck.
