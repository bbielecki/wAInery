# Architectural review gates

Status: **Gate 1 accepted**, **Gate 2 accepted** by the user (2026-10-31). Gate 3 component decomposition **accepted**; Gate 4 LLD in local review/commit process.

| Gate | Approval object | State / exit criteria |
|---|---|---|
| 1 — Repository layout | Directories, documentation and agent ownership | **Accepted** |
| 2 — Domain map | 8 contexts, owners, context map | **Accepted** — Decisions owns both predictions and action planning |
| 3 — Components | Per-module subcomponents, public ports/event schema drafts, dependency DAG | **For review**; must preserve business/cost/security guardrails |
| 4 — Full LLD | Exact schema, algorithms, class/API contracts, migrations, tests, ADRs | After Gate 3 approval; independent review and fixes |
| 5 — Publication | GitHub private branch / PR | After verification of repository permissions and artifact inspection |

Gate 3 user approval obtained. MVP is local, evaluation-first, with minimal running cost.

GitHub bbielecki/wAInery was previously not visible to the connected GitHub account despite recognizing user `bbielecki`; no repository changes made.
