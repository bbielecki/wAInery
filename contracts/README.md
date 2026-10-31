# Machine-readable public contracts (drafts awaiting Gate 3 review)

- `events/domain-change.v1.schema.json`: minimal typed event envelope with no raw proprietary text.
- `api/record-observation.v1.schema.json`: generic typed measurement or narrative observation input.
- `api/decision-plan.v1.schema.json`: plan output, alternatives, cited predictions and optional sourced probability.
- `tools/agent-advice-request.v1.schema.json` and `tools/agent-advice-result.v1.schema.json`: bounded model-neutral interface; results do **not** include invented numeric probabilities.
- `examples/*.json`: synthetic non-sensitive fixtures. Validate before freezing contracts.

**All schemas are design proposals, not compiled/production APIs.** Integration owner controls edits; module owners propose them through handoffs.
