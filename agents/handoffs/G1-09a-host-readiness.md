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

Fresh-main final tests, independent qa_g1_01 review (not C or IO) and CI pending
before squash. No owner handlers, weather, RAG, AI, plan evaluation or domain
operation E2E executed; not Gate1 claims. No privacy/rights/economic policy change.
No new transferable finding beyond canonical live/readiness separation; verified
blackbox behavior stays in regression tests. Existing verification skill reused.
Final demo/learning/admin prerequisite/human Gate1 review remains issue34.
