# LLD — Local runtime, lifecycle and budgets

## MVP process topology
One ASP.NET Core/.NET 10 local host registers eight modules in-process and runs `OutboxDispatcher`, `JobLeaseWorker`, `WeatherSyncWorker`, `DocumentImportCommand`, `LocalReportWriter` as hosted services/CLI commands. One PostgreSQL/pgvector in Docker Compose (local loopback only), plus protected local data folder. No separate broker, distributed cache, SIEM, managed object store or always-on GPU. Docling can be invoked as one-shot CLI via isolated Python environment. No public listener or authentication system in MVP; expose `127.0.0.1` only.

## Startup and readiness
Order: validate config/secret existence (do not log values) → PostgreSQL reachable/extensions present → migration version validated → dependency registry compiled and cycle-checked → detect expired job leases → check previously failed index publications → resume outbox → optional weather refresh if stale. On missing DB/secret, fail startup with diagnostic; on external LLM/weather outage, start in `DEGRADED` local analysis mode.

## Configuration keys
`WAINERY_DB_CONNECTION` (secret), `WAINERY_DATA_ROOT` (private directory), `WAINERY_AI_PROVIDER`, `WAINERY_AI_MODEL_ROUTE`, `WAINERY_AI_MONTHLY_BUDGET_USD` (default 5), `WAINERY_AI_DAILY_LIMIT_USD` (default 1), `WAINERY_AI_MAX_TOKENS_PER_RUN`, `WAINERY_WEATHER_LAT/LON`, `WAINERY_WEATHER_SYNC_MINUTES`, `WAINERY_EGRESS_DEFAULT=DENY`, `WAINERY_REPORT_PATH`. Use `.env.example` without values. Never commit real coordinates, operational notes, PDF or tokens. No hard-coded API keys.

## Budget envelopes
- Count `inputTokens`, `outputTokens`, provider/model, estimated+actual cost when available on every attempt, including failures and retries; store in `ai_execution.model_usage`, one idempotent record per request.
- Local rule/materiality gate precedes LLM; daily/per-scenario/monthly budget and max tool calls. When exhausted, status `BUDGET_EXCEEDED`, plan retains prior version with freshness caveat, L0/L1 risk detection continues.
- No token subscription assumed to cover API. Automatic model fallback allowed only if rights/egress and capabilities match; defaults fail closed.
- Use prompt template versioning, retrieval snippets only, hash/version cache keyed by subject+input revision+evidence index+model+prompt; never cache response across different rights workspace.

## Example compose topology (not a deployment manifest)
`db: postgres with pgvector extension`, `app: .NET host` with loopback port and volume mounts. Keep Postgres unexposed to public interfaces, use named DB volume and encrypted external backup. Developer can run app natively and DB in container; avoid hard dependency on Docker Desktop.

## Expansion path
At Connected Winery phase, add MQTT bridge/local edge collector with store-and-forward (not needed MVP). Later split a high-throughput ingestion worker and API deployment using same ports and outbox; move filesystem adapter to private object storage via `IObjectStorage`; add real RLS/tenant identities for SaaS. Keep schema changes additive and test migration/rollback plan. No claim that changing runtime/container topology is automatically zero-cost.
