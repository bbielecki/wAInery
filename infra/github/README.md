# Main protection ? admin action pending

2026-11-01 GETmain at73ef3c1: protected=false, enforcementoff, contexts[].
[Proposed JSON](main-protection.proposed.json) is unapplied; connector has no adminwrite.
Required by [trunk governance](../../docs/00-governance/trunk-based-collaboration.md#github-repository-settings-to-enable-human--admin).

Admin reviews payload then configures main in Settings?Branches/Rules: PR,1review,
stale/latest-push approval, admins enforced, strict checks, linear history,
conversation resolution, force/deletion blocked, no bypass. Exact Actions job
contexts: build-and-boundaries and PR rebased on main (not workflow display name).
General?Pull Requests: squash on, merge commits off, auto-delete heads on.
[REST payload semantics](https://docs.github.com/en/rest/branches/branch-protection#update-branch-protection);
restrictions=null for personal repo; inspect existing rules before replacing.

Verify GETbranch protected=true and admin GETprotection matches payload; future
PR must require checks/up-to-date/independent approval. Do not test by force/deleting main.
All connector PRs use bbielecki: distinct trusted authorized reviewer needed for
formal GitHub approval. Native review is separate; no fake second identity.
Record effective settings before human Gate1 approval. No token in Git/logs.
