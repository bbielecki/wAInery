# G1-09b final verification / issue34

IO / BR-10 / Gate1 #10 / base73ef3c1. Scope: reproducible synthetic demo,
accurate current docs and learning, reviewable unapplied admin proposal. No app/
tests/migration/wire schema/rights/budget change, no new dependencies. Quoted
public dotenv placeholder only; no real.env or passwords read/committed.

Files: AGENTS baseline, README; docs/80-delivery/monorepo-bootstrap.md;
infra/local/README.md and.env.example; infra/github/{README.md,
main-protection.proposed.json}; scripts/{README.md,demo.ps1};
agents/knowledge/{lessons.md,skill-registry.md}; scoped assignment, this evidence,
GATE-1-review.md andGATE-1-learning-digest.md.

Actual commands/evidence:

- `powershell -NoProfile -File scripts/demo.ps1 -Gate 1 -Offline`: exit0,
  Releasebuild0/0, architecture22 PASS, Pythonwirepositive/negative andskills PASS,
  full177/177 PASS (77contract+100actualPG/process), no skipped. No paid/liveweather.
- PowerShell Parser.ParseFile: no syntax errors; `-Gate 2 -Offline` rejects
  before execution. Proposed JSON shape strict/admin/one-review validated.
- ChangedMarkdown local file links and `git diff --check`: PASS.
- Synthetic temp dotenv with dollar/semicolon/quote: Composeconfig literal value
  retained (configserializes$as$$); DbConnectionStringBuilder parse roundtrip PASS,
  no values printed. Initial sandbox Dockerconfig inaccessible, rerun escalated;
  initial check wrongly used PSdictionary property setter, corrected to explicit
  .NETset_ConnectionString for verification only; documented GET was correct.
- Independent QA identified dotenv interpolation/shellprecedence P2: quoted
  placeholder andexplicitdotenvsyntax, inherited POSTGRES_PASSWORD guard added.
- Five knownmerged clean worktrees/localbranches removed only after exactSHA,
  knownsquash inmain, resolvedtemp-path/nonreparse andcleanstatus checks; noforce
  worktree removal. Now onlymain andactivefinalbranch, oneRootworktree.

Final independent qa_g1_01 READY FOR PR, no openP0/P1/P2 in changes; own static
AST/JSON/54links/regressionnames/whitespace PASS, no runtime execution by reviewer.
Fresh-main rerun/CI pending before squash. Current public
branchAPI protected=false; plugin noadminwrite. Payload does not enable settings.
Formal independent GitHub approval requiresdistincttrustedaccount; nativeQA
evidence is separate. Externaladmin/human gate remains #10 afterdeliveryartifact.
AppAPI0; codingcostunmeasured. Learning5verifiedentries,3existing skills reused,
no newskill or savings claim; B/CactualreceivingACKs captured. STOP forhuman
APPROVE GATE 1; no Gate2implementation.
