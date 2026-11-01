# G1-03c IO freeze work order

Issue27 / Gate1 #10; IO; BR-01/02/08/09/10; API0. Proposals22/23 by B/C
Stage Independently reviewed; final fresh-main regression and CI pending.
accepted with IO names/caps in issue27, before C test-first RED. Shared source
single-writer IO. No application handlers, migrations, wire schemas or grants.
Allowlist BB RightsStorageContracts.cs, Connectivity/Knowledge/AIExecution
Contracts, Weather scoped location read/resolution, tests/Contracts references/
EvidenceContractsTests, these proposals and scoped handoff/readmes.

Use BCL immutable values/System.Text.Json and existing Result/CommandContext/
SubjectRef/SourceVersionRef, no new dependency/framework. Current permissions
separate from historical AsOf; snapshots never authorize egress on their own.
Default DENY, unknown fail closed; policy widening only explicit owner validation.
Opaque public IDs and scoped object descriptors; real file/root/rights/current
lookup/as-of/pagination behavior requires future owner integration tests.
No fake implementations or claim of29 designed scenarios having run.
Technical read caps TopK1..50/tokens1..16384, not new model spend policy.
Independent reviewer differs from C test author. Fresh-main full suite+CI before
squash. Gate1 incomplete; subsequent Gates require human approval.
