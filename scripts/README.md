Bootstrap, local validation and contract checks. No network access required for documentation lint.

`powershell -NoProfile -File scripts/demo.ps1 -Gate 1 -Offline` runs actual
synthetic Gate1 checks, including disposable PostgreSQL and real child Host.
Prepare restore, Python validator environment and Docker images first; see
[Gate1 procedure](../agents/handoffs/GATE-1-review.md). Offline means no external
business/model/weather calls; it is not network isolation. The script does no
package restore, while Testcontainers can pull missing images via Docker.
It does not read user .env/private books or approve the next milestone.
