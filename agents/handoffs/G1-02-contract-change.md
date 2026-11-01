# G1-02 — common DomainChangeV1 contract proposal

Ticket #12 / milestone #10; owner and approving role: Architect / Integration Owner.
Human Gate 1 authorization: approve gate 0, 2026-11-01. BR-01/08/09.

Before: a wire v1 schema/example and incomplete, non-compiling C# sketches.
After: common immutable DomainChangeV1 plus a strict wire codec; no schema change.
The wire keeps workspaceId outside subject; the in-process SubjectRef carries
the same WorkspaceId. Aggregate/subject/source public IDs remain opaque strings.
All required fields, optional sourceVersion/timeRange, UTC instants, schema
version and known event/module values are checked at the boundary. Arbitrary
properties are rejected, including protected text; no domain payload in events.

Compatibility: first compiled representation of existing v1, no breaking wire
fields or enum changes. Existing fixtures remain producer/consumer regression
inputs. Shared ownership is unchanged; this approval is not a business/rights
policy change. Duplicate delivery semantics belong to the later outbox task.
Consumers: module event publishers and Decisions, only after this PR merges and
their branches rebase. No migration or paid model call is required.

Proof: .NET test project checks existing fixture, generated wire against the
existing JSON Schema, all required fields, invalid data, workspace mismatch,
opaque IDs and safe metadata. Tests are written before the implementation.
Independent review and fresh-main CI remain mandatory before the freeze.
