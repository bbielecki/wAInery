# G1-09b.1 architecture boundary regression

Issue36 split from34 / Gate1 #10; IO; BR-10; API0; base9810d8f PR37.
Allowlist Architecture/Program.cs, ArchitectureBoundaryTests.cs, testcsproj build-only
reference, scoped docs. B test5357e29 actualRED acceptedContracts→Persistence.
All moduleContracts projectrefs only exactBB target; preserve other guards/cycles,
handleCLIerrors exit1/FAIL rather than unhandledWER. No new packages, migrations,
module code, policy or business behavior. C reviewer differs from B testauthor/IO.
Synthetic copiedXML+builtchecker only, own child/tempcleanup. Freshmain/fulltests/CI
and independentreview before squash/delete. Finaldemo/KB/admin/humanstop issue34.
