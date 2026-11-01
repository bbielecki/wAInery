# G1-03c source/evidence/storage contract freeze

Issue27 / Gate1 #10; IO; BR-01/02/08/09/10; API0. Incorporates independently
prepared B/C proposals22/23. Pure IPC declarations/guards, not owner applications.
Reuse existing BB primitives, BCL immutable collections/streams/enum guards; no new
packages, service, provider, permissions, migrations or JSONv1 changes.

## Boundaries

Connectivity owns scoped source resolution/rights, receipt/process/replay and
installed adapter drafts. Registry/config revision differs from content and
mapping versions. Registration defaults DENY, draft targets cannot mark performed
work. Capture rejects foreign object workspace; actual identity/hash/schema/cursor
validation and owner dispatch remain future implementation.

Knowledge owns citations/document versions/indexes and stricter document rights.
Current permission is independent of historical AsOf. Rights snapshots record
metadata and never authorize a new call; Unknown/DENY/REVIEW fail closed, ALLOW
metadata requires proof and still needs current owner verification. Evidence
preserves document/source/index versions, distinct PDF/printed pages, nullable
locality, uncertain metadata, rights and knownAt. Query scope/UTC and technical
TopK1..50/token1..16384 caps are executable guards, not changed model spend policy.

IO object-storage port is workspace-scoped, key/hash/length/media/class metadata
only. Zero bytes differs from missing; hash shape is not actual content hashing.
Caller owns input stream and disposes successful output. Real root/symlink/path/
hash/length/access enforcement and non-seekable limits remain Gate4 tests.
Doc import rejects foreign object scope. Private bytes/excerpts never enter events.
Weather GetLocation/Resolve completes Vineyard's public binding seam. AIExecution
exports execution usage only, keeping actual billing nullable; reasoning stays
Decisions-owned. No foreign Contracts/implementation imports from Contracts.

## Actual proof

C executed BB-only RED before source; full/root RED missing Knowledge/EvidenceContext
after references integrated. C authored12 real constructor tests (cfd15c6→a4ad142),
IO added2 source/document scope cases. Latest52 contract PASS. Full111 PASS before
those2 additions (50contract+61 actualPG); build0 warnings/errors, architecture22.
Five historical schemas/two negatives, skills and whitespace PASS. Final full113
and fresh-main CI follow review/rebase; no claim these have run yet.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-restore
dotnet test --project tests/Contracts/wAInery.Contracts.Tests.csproj --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

No mocked retriever/storage/owner lookup presented as proof. Proposals'29 owner
scenarios are DESIGNED, NOT RUN, including egress revocation/as-of/path/citation
resolution and raw receipt replay. No OCR/embedding/model/weather/paid/live data.
Independent final reviewer differs from all source/proposal/test authors.
Independent /root/qa_g1_01 reread issue/workorder/proposals, all new source and14
tests; no open P0/P1/P2, READY FOR PR. Actual XML8 Contracts→BB/no packages, no
Persistence/Host/wire changes and whitespace PASS. Reviewer did not run .NET/PG.

## Risk, receiving owners and learning

DTO construction proves structural shape, not policy authority or existence.
Future owner handlers need reviewed additive ID/history/policy/index schemas and
real transaction/read tests; published migrations never edited. Consumers must
use owner ports after merge and acknowledge exact freeze on assignment. Relevant
handoffs: B Connectivity and C Knowledge proposals; Decisions #30 depends on BB
policy metadata only, never an implementation reference. Partial citations/knownAt
remain unknown; no invented source-quality probability or calibrated model claim.

Existing contract-change/verification workflow reused. No new transferable finding
or skill candidate: this freezes previously accepted boundaries, not verified RAG
quality/egress implementation. Measured savings unknown. Gate1 incomplete;
subsequent milestone work requires APPROVE GATE 1.
