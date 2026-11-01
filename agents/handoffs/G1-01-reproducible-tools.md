# G1-01 handoff — repeatable scaffold verification

- Ticket: [#9](https://github.com/bbielecki/wAInery/issues/9), milestone [#10](https://github.com/bbielecki/wAInery/issues/10).
- Owner: Architect / Integration Owner; BR-10, BR-08.
- Scope: repository NuGet source/mapping, nested project build globs, isolated
  pinned Python contract validator and instructions; Gate 0 approval recorded.
- Contracts: existing five v1 schemas/examples unchanged. No migration/outbox
  implementation, module behavior or data/rights policy changed.
- Changed paths: NuGet.Config, Directory.Build.props, .gitignore,
  tools/requirements-contracts.txt, tools/README.md,
  docs/80-delivery/monorepo-bootstrap.md, assignment and this handoff,
  agents/handoffs/GATE-0-plan.md (approval status only).

## Actual evidence (2026-11-01)

RED: ordinary solution restore returned NU1507 from inherited user sources.
After the source fix, build returned 64 CS0579 duplicate assembly-attribute
errors. Evaluated Weather Compile items included Contracts/obj/Debug generated
sources. Global jsonschema 3.2.0 previously failed importing Draft202012Validator.

GREEN, executed on SDK 10.0.100 / Windows:

```powershell
dotnet restore wAInery.slnx
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet msbuild src/Modules/Weather/wAInery.Modules.Weather.csproj -getItem:Compile -p:Configuration=Release -nologo
.venv-contracts/Scripts/python.exe -m pip check
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

All completed exit 0. Both builds: zero warnings/errors. Architecture: 19 valid
projects and no cycles/foreign implementation imports. Evaluated Compile items
contain no nested Contracts paths. Contract validator: five fixtures and two
negative cases PASS. Skills: positive/negative metadata self-test PASS.
DOTNET_CLI_HOME was directed to a task-specific temporary directory because of
the sandbox account; no user/global package configuration was changed.

The compiled Host was started as a hidden child process, queried using
Invoke-RestMethod http://127.0.0.1:5100/health/live, then stopped in finally.
Actual response: {"status":"ok","service":"wAInery","phase":"scaffold"}.
An earlier startup attempted before the build completed found no executable;
the completed-build startup above passed.

## Independent review

Native session /root/qa_g1_01 inspected the diff, NuGet source/mapping, compile
globs, all ten dependency metadata records and pip instructions. Independently
ran event validation plus invalid recordedAt and eventId cases (rejected), and
diff whitespace check. No P0/P1/P2 blockers; recommendation READY FOR PR,
conditional on latest-main freshness and CI. Suggested isolated public-index
pip instructions were applied. Reviewer did not write files or claim to run the
implementer's build/host checks.

## Limits, risks and learning

- PostgreSQL migration, unit/contract .NET suites and outbox: NOT RUN / not yet
  implemented. Docker daemon access was verified outside sandbox; this does not
  prove the planned database behavior.
- No paid model/weather calls; application API cost 0. No private inputs, source
  excerpts, provider credentials or real embeddings included.
- Transferable finding candidate: nested SDK projects require explicit source
  glob isolation; a clean-build-only check can miss generated child sources
  that break repeated builds. Evidence: CS0579, evaluated Compile before/after,
  two passing builds. Relevant to all module owners and contract freeze.
- Existing skills: wainery-pr-verification used. No new skill proposed from one
  occurrence; curated lesson may follow after PR/CI evidence is available.
- Remaining gate: latest-main rebase, CI and authorized integration. Next task:
  G1-02 compiled common envelope and deterministic producer/consumer tests.
- Gate 1 is incomplete. Gate 2 remains blocked pending APPROVE GATE 1.
