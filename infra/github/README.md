# Main protection: admin action pending

2026-11-01 read of GET /repos/bbielecki/wAInery/branches/main at9810d8f:
protected=false, enforcement_level=off, required contexts empty. This proposal is
not applied. The connected GitHub plugin supports repository/PR writes and public
GET, but no administration write; managed app lacks administration access.
No token collection or attempt to bypass account permissions.

Admin/owner prerequisite from [trunk governance](../../docs/00-governance/trunk-based-collaboration.md#github-repository-settings-to-enable-human--admin).
Review [proposed payload](main-protection.proposed.json) then configure main in
Settings → Branches / Rules. Require PR, one independent approving reviewer,
dismiss stale approval, latest push approved by someone else, both checks with
up-to-date branch, conversation resolution, linear history, apply to admins,
block force push/deletion. No bypass users/apps. Exact check contexts verified
from actual Actions jobs: `build-and-boundaries` and `PR rebased on main`; the
workflow display name `Monorepo structural checks` is not the first job context.
Contexts recently provided by GitHub Actions should be selected from that app.
For this personal repository restrictions=null; PR/admin enforcement prevents
direct pushes, rather than unsupported organization-only user restrictions.

In General → Pull Requests enable Squash merging, disable Merge commits, enable
automatic head deletion; existing cleanup workflow also deletes merged heads.
The JSON is a reviewable [REST update payload](https://docs.github.com/en/rest/branches/branch-protection#update-branch-protection),
not an installer. An already authenticated admin may PUT it to
`/repos/bbielecki/wAInery/branches/main/protection`; inspect existing settings first,
because an update can replace rules. No command here asks for a token in Git/logs.

Current connector PR author is bbielecki for every agent. Native independent
code review is recorded, but GitHub self-approval is unavailable: a distinct
trusted reviewer account with the required repository rights is needed for
enforced approving reviews. Do not use a second identity to fake independence.

Admin verification checklist:

- GET branch: protected=true; GET protection with admin access confirms settings.
- Both exact required check names, strict=true, one approval, admins enforced,
  force/deletion false; no bypass configured. Record time/effective rule evidence.
- A future small PR cannot merge before independent approval/checks, after main
  advances, or after a new push dismisses approval. Use disposable PR/test refs;
  do not test force/deletion by attempting destructive writes to main.
- Confirm squash/head deletion settings and authenticated distinct reviewer.

Until verified this is an external governance dependency, not a green enforced
Gate1 control. Human Gate approval and server protection are separate facts.
