# G1-09b.1 architecture CLI handoff

Issue36 / Gate1 #10, splitfrom34; IO BR-10; base9810d8f PR37; API0.
B authored2 blackbox fixtures5357e29 (Root14f53d0), actualRED53PASS/1FAIL.
Root current-main runtime reproduction75cases74PASS/1FAIL: checker incorrectly
accepted Weather.Contracts→Persistence with exit0. Positive copied graph PASS.
Root fix all moduleContracts ProjectReference targets to exactBB path, retaining
existing cross-implementation/outside-solution/cycle checks. Validation errors
handled byCLI FAIL/exit1, not unhandledWindowsWER. Root strengthened controlled-exit
assertion and added cross-implementation/cycle runtime negatives. No duplicate
test-sidecheckerlogic. Tests reuse BCL XML/process+xUnit, no newpackage.

Test project now builds actualchecker ReferenceOutputAssembly=false. Synthetic
repo copies only22 projectXML, solution andbuiltchecker/runtime/deps, no app source,
privateconfigs/documents or symlinks. Child andunique tempRoot bounded/owned cleanup.
Architecture validates projectreference graph, not every possible package/API
or future domain interaction. Existing owner rules remain canonical.

After firstfix75contracts PASS; after fresh-main fetch/rebase full177 PASS
(77contract+100actualPG/process), no skipped, Releasebuild0/0,
architecture22/schema/skills/whitespace PASS. Independent C staticreview READY FOR
PR atd6a057d, all4CLI fixtures checked, no openP0/P1/P2. Reviewer executed no runtime
tests. Fresh-mainCI required before squash. No application/migration/rights/
economic policy changes. Exact finalcommands:

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-build --no-restore
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Verified transferable finding: a negative graph must include shared infrastructure
targets; checking only another module misses non-module implementations. Runtime
negative fixture and deterministic CI are preferred over a new instruction skill.
Verification skill reused; no measured savings. Finaldemo/docs/KB/admin dependency
andhuman Gate1 review are separateissue34; no nextGate authorization.
