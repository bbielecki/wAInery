# G1-04 Decisions contract freeze handoff

Issue30 / Gate1 #10; IO; BR-01/03/04/09/10; API0. B proposal30fcf59 and18
targeted tests1d5f940 integrated in distinct commits. IO implements public IPC
only: no prediction/impact/ranking/model/storage algorithm or schema edit.
Reuse BB/BCL ImmutableArray/JsonElement, existing JsonSchema.Net test dependency.

Decisions owns IReasoningExecutor.AdviseAsync and immutable scoped request/budget/
provenance. Scalar facts clone JsonElement so document lifetime cannot invalidate
context; null/zero/false stay distinct, arrays/objects/Undefined rejected. Context
collections copy and cap per approved issue, only4 existing read tools. Missing
classification is unknown-denied; EVALUATION never bypasses policy.
Advice output is untrusted proposal, no numerical probability field. Plan's
0..1 probability requires structural provenance, preserves all3 calibration labels
including UNVALIDATED; metadata does not establish scientific validity or permit
publication as calibrated. Owner validators/presentation remain later work.

Definition-versioned scope and RequiresCoverageAudit preserve bounded unknown
Knowledge handling. Pending freshness is a separate IPC wrapper, never added to
existing PlanV1.state. CURRENT needs explicit catch-up/revision/version/expiry
conditions, not just a timestamp. No actual DB catch-up/CAS claim. Recompute/build
commands enforce context/workspace/target matching, copy scopes and normalize UTC;
critical fields get-only so ordinary with cannot bypass validation.

## Actual verification

B initial compile RED before source. First combined70-case run69PASS/1FAIL in test
helper: rebuilding same$id collided with global JsonSchema.Net registry. Supported
BuildOptions with local SchemaRegistry fixes isolation, no validation suppression.
Then70contracts and full155 PASS (70contract+85actualPG), build0/0,architecture22,
historical schemas/skills/whitespace PASS. Independent review found command scope
P1; two new cases reproduced runtimeRED70PASS/2FAIL (scope/UTC). New readonly
constructors/scope-copy guards then72contracts PASS. Review P2 optional prerequisite
arrays reproduced73cases72PASS/1FAIL; normalize default arrays to Empty both in
initialization and with-init, followed73contracts PASS. After fresh-main fetch/rebase,
full158 PASS (73contracts+85actualPG), no skipped, build0/0 andarchitecture22 PASS;
schema/skill/whitespace validators PASS. Independent qa_g1_01 final static review
READY FOR PR, no openP0/P1/P2, both findings closed. Reviewer did not execute
implementer's PG. Final PR CI must pass before squash.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Existing request fixture explicitly/manually maps to scoped IPC and owned fact
values. This is not a production decoder/default JSON serialization guarantee.
Existing JSONSchema negative cases reject Pending plan state and LLM probability;
no real model/provider call or decision evaluation executed. All data synthetic.
Actual feedback authorization, request/evidence matching, legal/safety constraints,
current rights and science remain owner implementation tests in approved gates.

No DB/outbox change, no source private data or new cost policy. Consumers AIExecution
and Decisions implement only after freeze merge/assignment acknowledgement.
Verified lesson candidates: isolate schema registries in repeated test builds;
validate relationships between valid scoped DTOs, not each value in isolation.
Evidence is reproduced tests/review, no performance savings claimed. Existing
contract/verification skills reused, no new skill. Gate1 incomplete;
next milestone requires APPROVE GATE 1.
