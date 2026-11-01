# G1-01 — reproducible local restore and contract validation

- Ticket: [#9](https://github.com/bbielecki/wAInery/issues/9); parent Gate 0: #8.
- Stage: Review, within human-approved Gate 1 (2026-11-01).
- Owner: Architect / Integration Owner; BR-10 and BR-08.
- Branch: agent/integration/g1-01-reproducible-tools, based on a41a77c.
- Allowlist: NuGet.Config, Directory.Build.props (nested project source globs), .gitignore (isolated validator environment only),
  tools/requirements-contracts.txt, tools/README.md,
  docs/80-delivery/monorepo-bootstrap.md, this assignment,
  agents/handoffs/G1-01-reproducible-tools.md and Gate 0 approval status.
- Non-goals: application code, public contracts, database migrations, user/global
  package configuration, warning suppression and paid APIs.
- Inputs/outputs: existing five v1 schemas and synthetic examples; unchanged.
- Dependencies: .NET 10.0.100 now available; Docker daemon accessible outside
  sandbox. No database changes are needed by this task.
- RED evidence: ordinary solution restore fails with NU1507 when multiple
  personal feeds are inherited; existing contract validator fails importing
  Draft202012Validator from global jsonschema 3.2.0. After restore, generated
  nested Contracts sources cause 64 duplicate assembly-attribute errors; source
  glob exclusion is an integration-owned prerequisite of a repeatable baseline.
- Design: repository-owned nuget.org source and mapping; pinned, isolated Python
  schema-validator dependencies, including date-time format support.
- Verification: ordinary restore, Release build, ArchitectureChecks, all existing
  schema/example positive and negative checks, skills validator and local host
  health response. No new mirror tests for configuration-only changes.
  Repeat Release build and inspect evaluated Compile items to verify nested
  Contracts sources are excluded while the separate Contracts projects compile.
- Cost: application API calls 0; no private documents, credentials or real data.
- Reused skills: wainery-pr-verification at final review; no contract-change
  procedure is needed because the public payloads remain unchanged.
- Independent reviewer: separate QA agent/session after the diff is ready.
- Final gates: fetch/rebase latest origin/main, executed checks, independent
  review and CI before integration. Gate 2 remains unauthorized.
