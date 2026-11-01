# Monorepo bootstrap — Gate1 technical foundation

**Status:** compiled ports, actual migrations/outbox/jobs/Host foundation tested;
human Gate1 review/admin protection pending. Working business decision vertical
and scientific validation remain later gates. [Current evidence/startup/demo](../../agents/handoffs/GATE-1-review.md)
separates actual behavior from [Business HLD](../10-architecture/business-hld.md) target design.

## Structure

```text
wAInery.slnx
Directory.Build.props
Directory.Packages.props
global.json
src/
  BuildingBlocks/        # stable cross-module value objects; Integration Owner
  Persistence/           # IO migrations, atomic outbox/inbox, durable leases
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
  Contracts/             # actual DTO/wire and architecture CLI regression tests
  Integration/           # actual Testcontainers SQL and child Host tests
  Modules/               # module-owned unit/integration tests
  Evaluation/            # 30–50 reviewed business scenarios + replay/baselines
infra/local/compose.yaml # loopback-only PostgreSQL + pgvector
```

Each module has a **Contracts project** and a **single implementation project**. The implementation project may contain separate `Domain/`, `Application/` and `Infrastructure/` folders/namespaces internally without multiplying assemblies for the MVP. Splitting a module into physical assemblies later is an inexpensive refactor when it has concrete benefits.

## Local developer commands

```bash
dotnet restore wAInery.slnx
dotnet build wAInery.slnx --configuration Release
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release
dotnet test --solution wAInery.slnx --configuration Release
```

**Prerequisites:** .NET10 SDK, Linux Docker engine (validated with Docker Desktop)
and Compose for manual local DB. Testcontainers integration uses disposable DBs;
no paid model/weather API or private document. [Exact PowerShell startup](../../agents/handoffs/GATE-1-review.md)
sets secret locally, explicitly migrates then starts loopback live/ready.
Normal Host connects and validates journal/extensions; it never auto-migrates.

The repository's `NuGet.Config` selects nuget.org explicitly and maps all packages
to that source. Personal feeds are not inherited, so Central Package Management
works without changing global configuration or suppressing NU1507. See
[contract validation](../../tools/README.md#contract-validation) for its isolated
Python environment.

Shared build settings exclude nested `Contracts/**`, `bin` and `obj` items from
the implementation project's default source glob. Contracts compile in their
own projects; repeated builds must not compile their generated assembly
attributes again inside the parent module.

## Ownership, dependency direction and contract freeze

- Integration Owner alone changes `wAInery.slnx`, `src/BuildingBlocks/**`, `src/Host/**`, root properties, CI, `contracts/**`, `infra/**`, initial migration assembly and shared test fixtures.
- Each module agent owns `src/Modules/<Module>/**` and `tests/Modules/<Module>/**` only.
- A module implementation can reference its own Contracts and external **Contracts**, not another module implementation. A module's Contracts assembly can reference only exact BuildingBlocks project, including no Persistence reference; actual negative CLI tests enforce this.
- Host is composition root; `Decisions` calls `AIExecution` behind the **Decisions-owned** reasoning contract; only Host wires it.
- SQL in docs/50-data/sql is reference design. Compiled immutable history under src/Persistence/Migrations is authoritative for execution; four migrations verified on actual PG, including upgrade/rollback. Never edit published migration bodies. IO owns shared DDL.
- Public subject/source identifiers remain opaque strings and map to internal UUIDs in owner modules. No leakage of EF entities between contexts.

## Current evidence and remaining business gates

1. Gate1 compiled owner/advice/evidence/storage ports and v1 fixtures, actual migration/integrity/outbox/inbox/job/Host tests are present. No actual domain command writes yet: atomic synthetic publication proves the infrastructure seam.
2. Independent reviews and green PR checks recorded; admin-enforced main protection remains external human action. Human Gate1 approval required before implementation Gate2.
3. Later: working Weather→scoped invalidation→plan/report, confirmed domain commands, current rights/egress and cost enforcement,30–50 reviewed business scenarios. See approved [roadmap/DAG](../../agents/handoffs/GATE-0-plan.md); technical health is not business acceptance.

## Why monorepo without Nx or a broker?

For a single local host and eight logical modules, .NET solution/project boundaries
are sufficient. CI also runs actual contract/SQL/process/negative graph suites and
wire/skill validators. No additional orchestration service/broker needed. Optimize
test selection only after a measured bottleneck.
