# Architecture guardrails — accepted business purpose

Status: **Gate 3 component proposal**. Applies to every agent, ADR, schema, implementation and review.

## Business objective and constraints (priority order)

1. Deliver **actionable, Polish-contextualized** recommendations about vineyard/cellar work and *when* to act; include alternatives, uncertainty, constraints and evidence, not polished but unsupported prose.
2. **Demonstrate quality in MVP** with 1–2 private scanned books, curated internet knowledge, descriptive vineyard/cellar history, and one free forecast provider. No large-scale IoT, public access, or real-time operation required.
3. Keep **marginal and fixed running costs tiny**: local .NET host + PostgreSQL/pgvector + local files; metered external text model only where decision quality warrants. No Kubernetes, Kafka, Redis, object-store server, managed vector database, always-on GPU or paid crawler in MVP.
4. Maintain domain/knowledge provenance and source rights. Preserve originals and lineage; external prompt data has an explicit policy; no protected content, raw records, credentials or real embeddings committed to Git.
5. Architectural evolution must support additional instruments (weather station, tank temperature, pH, Brix, titratable acidity, laboratory data), more providers/models, specialized agents, UIs, and notification channels without destructive rewriting of durable truth.
6. Accept **cheap additive schema migrations**; reject abstract JSON-only entities and expensive forced migration avoidance. One PostgreSQL cluster with per-context schemas.
7. **On every accepted update**, invalidate only potentially dependent projections/predictions; incrementally refresh when material or due. Only material, unresolved decision changes invoke LLM; use local rules for cheap checks, no unconditional multi-agent fan-out.
8. Probability must have verifiable statistical/expert-model provenance and validation. LLM-generated percentages without such provenance are forbidden. Unknown probabilities are nullable, not zero.
9. The AI recommends; the user confirms an actual operation. Natural-language note extraction is an untrusted draft until accepted.
10. Local-only MVP still needs secret isolation, minimal backup, rights labeling and bounded agent tools. No internet exposure requirement until later phase.

## Global component contract

- Interfaces use stable IDs (`WorkspaceId`, `SubjectRef`), UTC time, source timestamp and arrival timestamp, version, correlation/causation identifiers, and idempotency keys where applicable.
- API and events have semantic versions and realistic JSON Schema examples; event envelopes do not contain copyrighted text or arbitrary prompt content.
- Module data stores are exclusively writable by the owning module. Read surfaces are typed query ports and/or rebuildable projections.
- Evaluation includes 30–50 expert-reviewable scenarios, incremental recomputation coverage, regional suitability, decision accuracy, citation quality, and cost per case.

## Exit criteria and traceability

See `docs/70-quality/gate3-component-review.md` and `docs/80-delivery/implementation-dag.md`. Changes to any guardrail require explicit ADR and impact to MVP cost/quality and future extension.
