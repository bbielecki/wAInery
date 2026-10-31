# Modular monolith — implementation project boundaries

**Gates 1–3 accepted.** Each of the eight modules has two .NET 10 projects:
- `Contracts/wAInery.Modules.<Module>.Contracts.csproj`: public ports, immutable DTOs/events, only BuildingBlocks reference.
- `wAInery.Modules.<Module>.csproj`: the domain/application/infrastructure implementation; it may reference other **Contracts** projects, never another implementation.

Seven module teams may implement concurrently **after** Wave 0 freezes compiled C# DTOs and running migrations. `Decisions` consumes read contracts of the other business modules; `AIExecution` implements the `Decisions` reasoning port behind Host DI, not by direct dependencies.

Do not add microservices, internal cross-module EF references or generic shared domain entities just for modularity. The business objective remains high-quality next-action/time recommendations at minimal operating cost. See [monorepo bootstrap](../../../docs/80-delivery/monorepo-bootstrap.md) and [business HLD](../../../docs/10-architecture/business-hld.md).
