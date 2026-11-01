# G1-02 work order

Ticket #12; milestone #10; BR-01/08/09; API budget 0; Integration Owner.
Stage: Review. Upstream #11 merged. Branch agent/integration/g1-02-domain-change-v1.

Approved integration proposal: agents/handoffs/G1-02-contract-change.md.
Allowlist: src/BuildingBlocks/**, tests/Contracts/**,
tests/Architecture/Program.cs, Directory.Packages.props, global.json (native .NET 10 test runner), wAInery.slnx,
.github/workflows/monorepo-build.yml, this assignment and scoped G1-02 handoffs.
Non-goals: module implementations, migration/outbox, new wire versions or enum
values, domain safety thresholds and changes to rights/egress.

Freeze existing DomainChange v1 fields via explicit wire/scoped mapping. Use
tests/Contracts with deterministic synthetic fixtures and schema validation.
RED: the new consumer tests cannot compile because the common event/codec does
not exist. GREEN requires positive/negative tests, whole solution build,
ArchitectureChecks with extra test projects allowed, historic schemas and CI.
Compiler globs must continue to isolate public Contracts projects. A separate
QA session reviews before integration. No downstream feature starts before
the contract PR merges. Gate 2 remains unauthorized.
