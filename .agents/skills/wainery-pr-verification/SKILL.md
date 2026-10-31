---
name: wainery-pr-verification
description: Verify a wAInery implementation PR before review or merge, with real test evidence, rebase and trunk branch checks.
---

# Verify one wAInery PR

**Use when:** an implementation branch or review PR is ready for validation. **Do not use** to claim actual PostgreSQL, LLM or business tests pass when corresponding projects/services are absent. This skill does not authorize merge.

1. Read root `AGENTS.md`, [trunk rules](../../../docs/00-governance/trunk-based-collaboration.md), [testing strategy](../../../docs/70-quality/implementation-test-strategy.md) and the PR's owning module/spec. Identify touched contracts, migrations and rights-sensitive areas.
2. Ensure the branch is isolated and unshared. Fetch `origin/main`; **rebase only when you own the branch and it is safe**. If `main` moved after the last test, request/perform a new rebase and rerun checks. Never force-push `main`.
3. Execute, if environment supports it:

   ```sh
   dotnet restore wAInery.slnx
   dotnet build wAInery.slnx --configuration Release
   dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj
   ```

   Add the actual applicable module unit, contract and Testcontainers PostgreSQL tests if they exist. Do **not** substitute a mocked migration for real SQL proof. No paid AI/weather calls in ordinary CI.
4. Inspect `git diff origin/main...HEAD`: boundary ownership, contract versioning, privacy leaks, unsafe source transfers, stale prediction handling, replay/idempotency and unreviewed cost. Use synthetic fixtures.
5. Ask an **independent reviewer** to inspect code and tests. Capture commands actually run, exit codes, CI URL, PASS/FAIL/SKIPPED with reason, and unresolved P0/P1.
6. Output a compact PR verification report: branch/head/main, business BR, affected modules/contracts, evidence links, unrun tests and recommendation **READY FOR REVIEW / BLOCKED**. **Do not self-approve, auto-merge or delete a branch.**

**Stop:** failing mandatory check, unreviewed shared change, main drift, private data in diff, untested critical DB migration or unauthorized cost/egress. Do not weaken gates to turn the report green.
