# G1-09a Host technical readiness

Issue32 / Gate1 #10; IO; BR-09/10; base b3988d8; API0. Allowlist Host,
Persistence read-only compatibility probe, HostReadinessTests, scoped docs.
No migrations, worker/business endpoints, prices/rights changes or new package.
C authored13 blackbox cases in isolated worktree, initial actual runtime RED;
IO implements composition, independent QA differs from C and IO. Existing ASP.NET
Core10 health middleware/DI and NpgsqlDataSource reused. Exact behavior in issue32:
startup config+journal/vector check, no auto-migrate; live200, ready200/503;
validated HTTP loopback URL only, alternative endpoint overrides cannot publish.
No private exception details in responses/logs. Fresh-main full suites/CI/review
before squash, final demo/KB/admin verification separateissue34. No Gate2.
