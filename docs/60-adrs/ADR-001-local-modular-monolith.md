# ADR-001 — Single local modular monolith with PostgreSQL

**Status:** Proposed, Gate 3. **Context:** 1 user, 1–2 books, weather API, manual histories, minimize spend, preserve future scale.

Decision: one .NET 10 host with modules split by ownership; one PostgreSQL/pgvector database with logical schemas; local protected files; no broker, Kafka, Redis, Kubernetes or SaaS-hosted vector DB. Each module has public typed contract surface and private persistence. Application host assembles ports.

Consequences: tiny fixed cost and easier local integration, but one failure domain and scaling limits accepted in MVP. Migration to managed infrastructure is additive, not a promise of zero refactoring. Revisit when measured throughput, uptime or tenant isolation requires it.
