# Freeze checklist — independently implementable .NET modules

Status: **pre-implementation contract specification; signatures below are design sketches, not currently compiling assemblies**. Integration Owner freezes v1 DTOs and public ports first. See [ports](ports-and-compatibility.md), [assignments](../../agents/assignments/README.md) and [parallel delivery](parallel-delivery.md).

## Module graph / ownership rules

- `src/BuildingBlocks`: ids, Result/Error, common clock, unit primitives, outbox/inbox infrastructure contracts. **No domain entities or EF table access**.
- `src/Modules/<Module>/Contracts`: exported read models/commands/events only; versioned assemblies or namespaces.
- `src/Modules/<Module>/Domain`: invariants, entities, policies.
- `src/Modules/<Module>/Application`: commands, handlers, public ports and transaction ownership.
- `src/Modules/<Module>/Infrastructure`: EF mapping, adapters and own schema only.
- `src/Host`: composition root / CLI / local HTTP; cannot contain decision logic.
- `contracts/`: machine-readable serialized wire contracts, Integration Owner controlled.

No `Infrastructure` reference to a different module's `Infrastructure` or `Domain`. Cross-module calls through `Contracts` and explicit read ports; integration events through versioned envelope. Prevent direct EF navigation across module schemas.

## Common value objects (freeze in Wave 0)

```csharp
public readonly record struct WorkspaceId(Guid Value);
public sealed record SubjectRef(string Module, string Kind, string Id); // public opaque ID
public sealed record EvidenceRef(string SourceId, string Version, string Locator);
public sealed record TimeWindow(DateTimeOffset From, DateTimeOffset To); // half-open [from,to)
public sealed record Quantity(decimal Value, string Unit, string? Method);
public sealed record DomainError(string Code, string Message, bool Retryable, string CorrelationId);
public sealed record Result<T>(T? Value, DomainError? Error);
```

All public IDs remain **opaque strings**, though internal PostgreSQL IDs are UUID. Owner maps `(workspace,module,kind,externalId)` to internal UUID. No runtime `Guid.Parse` on a `SubjectRef.Id`. `TimeWindow` requires `To > From` and UTC persistence; timezone used only for display and interpretation of user input. Use decimal for domain measurements, not floating-point primary records.

## Public port matrix

| Module | Writes/commands (owner only) | Immutable reads | Events |
|---|---|---|---|
| Connectivity | `RegisterSource`, `CaptureReceipt`, `ProposeOperation`, `ConfirmProposal` (delegates to owner) | `GetReceipt`, `GetSourceStatus` | `SourceReceiptCaptured` internal; canonical owner event only after confirmation |
| Vineyard | `UpsertBlock`, `ConfirmPhenology`, `ConfirmVineyardOperation`, `CorrectOperation` | `GetBlockSnapshot`, `GetConfirmedHistory(asOf)` | `VineyardProfileChanged`, `PhenologicalStageConfirmed`, `VineyardActivityRecorded` |
| Winery | `CreateBatch`, `RecordVesselOccupancy`, `ConfirmWineryOperation`, `CorrectOperation` | `GetBatchSnapshot`, `GetOccupancyAt` | `WineBatchStateChanged`, `WineryOperationRecorded`, `VesselOccupancyChanged` |
| Observations | `RecordObservation`, `CorrectObservation`, `RegisterMeasurementType` | `GetObservationWindow(asOf)` | `ObservationRecorded`, `ObservationCorrected` |
| Weather | `PublishForecastRun` | `GetForecastAsKnownAt(asOf,validWindow)`, `GetForecastDiff` | `WeatherForecastPublished` |
| Knowledge | `ImportDocument`, `PublishIndex`, `UpdateRights` | `SearchEvidence`, `ResolveCitation` | `KnowledgeIndexPublished`, `DocumentRightsChanged` |
| Decisions | `InvalidateScope`, `Recompute`, `RecordFeedback` | `GetPredictionState`, `GetPlan`, `ListOpenActions` | `PredictionUpdated`, `DecisionPlanPublished`, `DecisionPlanSuperseded` |
| AIExecution | no business writes | `ExecuteAdvice`, `GetUsage` | internal run usage only |

**Note:** some command names above have no v1 wire schema yet. Until the Integration Owner freezes them they are **proposed in-process ports**, not implied existing HTTP endpoints. `ConfirmProposal` must call the appropriate canonical domain owner and may not write another module's tables.

## Cross-module transaction boundary

1. Owner command validates subject/workspace mapping, idempotency, method and invariants.
2. In same PostgreSQL transaction: canonical business write, new aggregate version, `processing.outbox` row with safe `DomainChangeV1` metadata (not copyrighted text).
3. Dispatcher delivers at least once. Consumer inserts inbox idempotency record and schedules scoped invalidation **transactionally**.
4. Read views expose `PENDING_REEVALUATION` while canonical changes have not yet passed scoped impact handling; only a revision-matched result becomes `CURRENT`.
5. AIExecution sees context *only* through policy-filtered request; never mutates canonical facts.

Concurrency: database uniqueness is ultimate idempotency guard; retries have bounded attempts, lease and dead-letter state. New revision arriving during LLM invocation must make compare-and-swap publication fail and reschedule.

## API / CLI command conventions for MVP

- Minimal local-only interface may be CLI first; calls to public ports use `WorkspaceId`, command `IdempotencyKey`, `CorrelationId`, `ActorId` (local owner) and `ExpectedVersion` where applicable.
- Operations that change real-world performed state require an explicit confirmation token referencing the extracted proposal and owner-verified target/time.
- Returned `Result<T>` has codes: `VALIDATION`, `NOT_FOUND`, `CONFLICT`, `POLICY_DENIED`, `UNAVAILABLE`, `RATE_LIMITED`, `BUDGET_EXCEEDED`, `RETRYABLE_FAILURE`.
- List reads are bounded and paginated. Every as-of result reports `observed/issued/retrieved/recorded` timestamps as applicable and `knownAt` watermark.

## Adapter/provider contract expectations

- Model capabilities and costs are inputs, not hard-coded model enum in business modules; change provider without changing `Decisions`.
- Weather provider may omit true issued timestamp: persist NULL `issued_at` plus `issued_at_basis=UNKNOWN` and compare `retrieved_at` for known-time replay. Never fabricate issued time.
- Retrieved evidence includes rights class; deny external egress by default for private book chunks.
- Local object storage path must be hashed/normalized and protected from traversal, ready for later S3 replacement.

## Pre-merge contract gates

1. Each sample conforms to its frozen JSON Schema v1, including negative tests for extra arbitrary properties.
2. Producer/consumer test `SubjectRef` resolution, unknown IDs, mixed-workspace and as-of behavior.
3. Outbox integration test enforces same-transaction write and exactly-once **business effect** under at-least-once delivery.
4. Race test proves old LLM run cannot publish a current plan after newer input.
5. No inference of real-world completion from AI extraction alone.
6. Private content leak test verifies egress policy and logs.
7. Migration SQL tested on actual PostgreSQL+pgvector in Testcontainers **before** implementation release; reference SQL in repo is not yet proof of compatibility.

## Agent handoff required fields

Each PR/handoff includes: owner, changed files, API/DB schema deltas, event version, fixture IDs, test proof, benchmark effect, token-cost impact, privacy impact, dependent modules and rollback. Shared contract changes are proposed separately and merged by Integration Owner **before** consumers depend on them.
