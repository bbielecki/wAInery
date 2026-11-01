# G1-09a technical Host readiness handoff

Issue32 / Gate1 #10, IO BR-09/10; base b3988d8 (PR35), API0. C test commit
0771a58 cherry-pickedfb98484, only13 actual process scenarios. IO source under
Host/Persistence, no module/migration/wire/package changes, no worker registration.

Normal Host requires private WAINERY_DB_CONNECTION, validates compiled journal
names/checksums/count and vector/btree_gist in one read-only statement/snapshot.
Probe bounded5s including connection; no ApplyAsync during startup/health. Shared
DI-owned NpgsqlDataSource. Await-disposed WebApplication owns factory registrations.
HTTP loopback URL only, default127.0.0.1:5100, override WAINERY_LISTEN_URL can use
dynamic127.0.0.1:0. All Kestrel:Endpoints config rejected before listen because it
can supersede UseUrls. Built-in live/ready endpoints, plain safe status, no private
exception attached to health logs/results. Required startup failure exits1 safely.
--migrate behavior preserved, explicit migration remains operator command.

Framework sources checked: [ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0),
[Npgsql data source](https://www.npgsql.org/doc/basic-usage.html).
No custom health framework/server/provider dependency. Technical readiness checks
journal/extensions, not every physical table, actual business/catch-up or model
availability. No claim of full approved runtime lifecycle implementation yet.

## Actual evidence

C initial real RED oldHostlistenswithoutDB (not timeout); own child cleanup PASS.
After IO source full171 PASS:73contract+98actualPG/process (includes13Host), no
skipped; Releasebuild0warnings/errors, architecture22, wirepositive/negative,
skills andwhitespace PASS. Tests start actual hidden owned dotnet child with
isolated env and disposable DB. Missing/malformed/unmigrated/changedjournal/vector
rejected beforelisten with safeexit1; invalidURL3cases rejected. Valid dynamic
loopback aftermigration live/ready200; journalchange→ready503/live200;
journalrestore→ready200. Four adversarial env/CLI URL/Endpoint configuration paths
cannot bind public listener. No fake HTTP handler or paid/liveweather call.

```powershell
dotnet build wAInery.slnx --configuration Release --no-restore --verbosity quiet
dotnet test --solution wAInery.slnx --configuration Release --no-build --no-restore --verbosity quiet
dotnet run --project tests/Architecture/wAInery.ArchitectureChecks.csproj --configuration Release --no-build --no-restore
.venv-contracts/Scripts/python.exe tools/validate-gate3-contracts.py
python scripts/validate_agent_skills.py
git diff --check
```

Fresh-main fetch/rebase then full173 PASS (73contract+100actualPG/process), no
skipped, Releasebuild0/0, architecture22/schema/skills/whitespace PASS. Independent
qa_g1_01 final staticreview READY FOR PR; both P1/P2 below closed, no openP0/P1/P2.
Reviewer did not execute implementer's PG. PR CI pending before squash.
No owner handlers, weather, RAG, AI, plan evaluation or domain
operation E2E executed; not Gate1 claims. No privacy/rights/economic policy change.
Independent review found startup-only endpoint guard can be bypassed by default
Kestrel endpoint reload. New owned temp contentRoot test changed its own JSON after
startup, adding only a second LOOPBACK endpoint (no public exposure during RED).
Actual single-case RED: unapproved listener answered health HTTP200, not a timeout.
Explicit Configure(KestrelSection,reloadOnChange:false) disables this path while
preserving startupreject. Regression observes8s and then verifies original ready.
Single-case GREEN after fix, actual1PASS. P2 added actual SQL exception coverage:
own disposable journal temporarily renamed, view/function raises synthetic private
marker; ready503/live200 then restore→ready200, marker/password/connection/function
absent from response/logs. Actual filtered1PASS; source catch already correct.
Now15Host cases, final173/full rerun PASS. Transferable lesson candidate: one-time configuration
validation is insufficient if framework reload can change the validated resource.
Existing verification skill reused. No new skill; deterministic regression is better.
Final demo/learning/admin prerequisite/human Gate1 review remains issue34.
