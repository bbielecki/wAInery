# Event contract proposals

`domain-change.v1.schema.json` captures version, effective/received time, source reference and domain scope. Event type is typed and payload limited to IDs/changed fields; protected text stays in owning context. Delivery is at-least-once via Postgres Outbox; consumer is idempotent.
