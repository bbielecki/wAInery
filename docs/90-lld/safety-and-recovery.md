# LLD — Minimum security, IP, self-healing and reliability

## Threat model MVP
Assets: private copyrighted scans, OCR/chunks, operational notes, location, provider keys, agent tools and forecasts. Threats: accidental exposure in Git/logs/provider requests, malicious URL/prompt injection, infected PDF/OCR artifacts, incorrect domain write, model hallucination, stolen API token, duplicate jobs, hardware shutdown/data loss. Attack surface MVP is local CLI/localhost API, file imports, provider HTTP, local Postgres and external AI API. This is a reduced risk envelope, not zero security.

## Controls
- Local bind only; credentials read from env/secret store, no committed `.env`, user data encrypted by OS full-disk encryption if available, restricted file permissions.
- PDF scanner/URL fetch sandbox: max file size/page count/time, SSRF protection (reject localhost/private IP/metadata endpoints, follow redirects only through validated DNS; no arbitrary unaudited URLs), safe path normalization, no shell interpolation, disable embedded script execution. Read only curated public sources.
- Vault `PRIVATE_COPYRIGHT` by default; per-source `external_ai_policy` enforced before prompt build and again at provider gateway; if DENY, no copyrighted segment sent externally. Also apply policy to derivative passages. No hosted full-book store. Citations in reports are references to local assets; don't email protected excerpts.
- Contextual source text is **untrusted**: do not promote web/PDF content to system messages, no tool invocations selected solely from retrieved instructions, tool allowlist per role, strict output schema, hard max tokens/steps/cost.
- Provenance and policy decisions logged as IDs/hashes, not raw text. At least one backup copy of DB + canonical raw files with restore test. `.gitignore` covers raw and secrets; CI scans for accidental key exposure when introduced.

## Operational recovery table
| Failure | Detection | Recovery | Effect on user |
|---|---|---|---|
| Weather HTTP unavailable | timeout/429/stale | bounded exponential backoff + last forecast | `STALE`, no ungrounded weather probability |
| LLM timeout | gateway timeout | single retry, approved fallback | `FAILED/INSUFFICIENT`, local risk checks continue |
| OCR malformed | document ingest quality | quarantine + retry pinned parser | old knowledge version remains active |
| Worker dies after DB write | lease expiry / unacked outbox | reacquire, dedupe, compare revisions | eventual consistency, no duplicate action |
| Recompute input changes mid-LLM | compare-and-swap failure | discard obsolete run, coalesce latest | old answer not published as current |
| Source note extracted wrongly | confirmation gate | require correction from owner | no fake completed operation |
| Out of token budget | ledger policy | skip optional agents | clear `BUDGET_EXCEEDED` status |
| DB corruption/host loss | backup heartbeat, restore drill | restore raw + DB, rebuild indexes | unavailable until restore |

## Alerting design future
A notification channel interface exists, but no external emails required for MVP. Only persisted plan changes and critical policy alerts are notification events; deduplicate on `(planId,revision,eventType,channel)`, apply quiet hours and priority. A mail channel must never require private OCR content. When local host off, no guarantee of continuous alerts; startup should label backlog and show known-to-system time.

## Audit and observability
Log `correlationId`, `causationId`, `workspaceId`, subject, source version, retry count, model route, tokens, cost, latency, prediction state, plan revision; redact raw prompt, PDF, document snippets, access tokens, full notes and precise geolocation. OpenTelemetry spans optional in MVP; structured file/console logs sufficient with future adapter. Collect success/failure metrics of jobs, stale queue age, missing weather, index health, validation rejects and cost ceilings. Data security reviews required before public access.
