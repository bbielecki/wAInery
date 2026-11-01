# G1-06 integrity handoff

Issue #18 / milestone #10; Integration Owner; BR-08/09/10; API cost 0.
Scope: additive migration 002, actual PostgreSQL integrity/upgrade tests and
catalog-count-neutral migration tests. Published 001 has no diff. No public DTO,
module implementation, threshold, rights or economic policy changed.

## Behavior

Vessel occupancy excludes overlapping half-open [from,to) ranges within workspace
and vessel; NULL end is open. Adjacent ranges and other vessels remain legal.
Correction references in Vineyard, Winery and Observations require the same
workspace and cannot reference their own row. Prediction definition payload and
identity cannot change, and persisted publications cannot be deleted/reinserted;
retire using active=false and publish a new version. There is no draft state in
the existing catalog. This is metadata provenance enforcement, not model logic.

Upgrade never repairs/deletes inconsistent historical data. A conflict rolls back
all pending DDL/journal writes and preserves records; operator resolution needs a
separately reviewed work order. Normal data commands/as-of remain future tasks.

## Evidence actually executed

RED: 8 integrity cases failed because no exception was thrown before 002, while
9 prior integration cases passed. Review found unused definitions could still be
deleted: new DELETE case reproduced RED (20 PASS, 1 FAIL) before the fix.

Final local suite: 53 PASS, 0 failed/skipped (32 contract, 21 real PostgreSQL).
Release build zero warnings/errors; architecture validates 22 projects. Historical
five JSON examples/two negatives, skill validation and whitespace checks PASS.
SDK 10.0.100 / Windows; fixture-owned disposable pgvector:pg17, synthetic data.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Integration/wAInery.Integration.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Actual cases include old valid history surviving 001→002, old overlapping rows
blocking upgrade with journal still at 001 and both rows retained, open occupancy
blocking future use until closed, adjacency, three owner positive/cross-workspace
corrections, three not-self cases, used payload update rejection, unused DELETE
rejection, new version publication and active selection changes.

Independent /root/qa_g1_01 found missing DELETE protection and missing nullable
occupancy coverage. Both fixed test-first; final rereview found no open P0/P1/P2,
and parent rerun passed all 53 tests. Fresh-main CI must pass in the PR before
merge. Reviewer performs source/static checks and does not claim
parent's PostgreSQL executions. No paid APIs or live weather/private documents.

## Risk, demo and learning

Run the integration command above for reproducible positive/negative synthetic
demonstration. New DB migrates two versions, repeat CLI applies zero. Migration
may fail on existing invalid data and briefly locks affected tables; no automatic
repair or data deletion is provided. Test rollback is not backup/restore evidence.
PostgreSQL semantics: https://www.postgresql.org/docs/17/rangetypes.html and
https://www.postgresql.org/docs/17/btree-gist.html.

Verified transferable candidate: version immutability must cover DELETE+INSERT
for unused versions as well as UPDATE/FKs; test real upgrade with existing data,
not only creation of empty schemas. Relevant receivers: Winery, Observations,
Vineyard and Decisions owners; acknowledgement remains pending until assignment.
No new skill proposed; existing verification skill covers the review workflow.
No measured savings claimed. Atomic outbox, replay/jobs and remaining contract
seams are still pending. Gate 1 is incomplete; Gate 2 remains unauthorized.
