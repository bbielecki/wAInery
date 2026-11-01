Local PostgreSQL/pgvector is in compose.yaml; Host runs natively and binds loopback.
Copy .env.example to .env only if absent, edit a private password, then follow
[exact startup/demo commands](../../agents/handoffs/GATE-1-review.md). Never commit
.env. Explicit --migrate applies compiled history; normal Host never migrates.
Default DB service binds127.0.0.1:5432 and persists in its named volume. Tests/demo
use separate disposable Testcontainers databases; they do not use this volume.
No paid model or live weather needed for technical health. Do not delete a user
volume to resolve a connection/configuration error.
