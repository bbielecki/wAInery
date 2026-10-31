# Contract catalog / public surface (Gate 3 proposal)

## Ownership convention

The owning domain publishes **commands and queries as explicit public DTOs**; only the owning code reads its own schema. The integration owner maintains machine-readable shared contracts in `/contracts` after producer/consumer review. No agent edits them unilaterally. Version changes must be additive or accompanied by migration and compatibility fixtures.

| Boundary | Producer | Consumer | Contract / cardinality | MVP |
|---|---|---|---|---|
| Source receipt | Connectivity | Knowledge/Weather/Vineyard/Winery/Observations | typed `ImportProposal` with receipt/version reference | yes |
| Vineyard facts | Vineyard | Decisions | `VineyardActivityRecorded`, `PhenologicalStageConfirmed` | yes |
| Winery facts | Winery | Decisions | `WineryOperationRecorded`, `WineBatchStateChanged` | yes |
| Observations | Observations | Decisions | `ObservationRecorded`, `ObservationCorrected` | yes |
| Weather changes | Weather | Decisions | `WeatherForecastPublished` | yes |
| Knowledge state | Knowledge | Decisions | `KnowledgeIndexPublished` (with affected topic/variety/phase tags), `DocumentRightsChanged` | yes |
| Decision review | Decisions | AIExecution (via `IReasoningExecutor`) | `AgentAdviceRequest/Result` with typed candidates | yes |
| Plan output | Decisions | local report, future Notification | `DecisionPlanPublished` with planId/revision/ref | yes |
| Plan queries | Decisions | Local CLI/API | `DecisionPlanV1` read DTO | yes |
| Telemetry readings | future sensors via Connectivity | Observations | `RecordObservationV1` | later source, MVP manual |

## Cross-cutting envelope

`DomainChangeV1`: `eventId` UUID, `eventType` (producer + past-tense action), `schemaVersion`, `workspaceId`, `aggregateId`, `aggregateVersion` positive integer, `subject`, `effectiveAt`, `occurredAt`, `recordedAt`, `correlationId`, `causationId?`, `changedFields[]`, `dataRef` (opaque, **not** free content), `sourceVersion?`. Payload, when needed, is event-type-specific versioned schema. See `contracts/events/domain-change.v1.schema.json`.

## Data port conventions

- `SubjectRef`: `(module, kind, id)`, strongly validated; not database foreign key across contexts.
- `Quantity`: decimal + unit; metadata includes measurementType/method and original reading. No implicit pH ↔ acidity ↔ Brix conversion.
- API writes use `idempotencyKey`; errors have `code`, `message`, `retryable`, `details` with no secrets.
- Read/query ports require `WorkspaceId`, `asOfUtc`, optional paging limits. `AsOf` means known-to-system at that time, not simply effective timestamp.
- Recommendation candidate has `kind`, `subject`, `actionType`, `dependencies`, `exclusivityGroup?`, feasible/constraint findings, timing window, human-approval requirement, and citations. Ranking is performed in Decisions.
- `eventProbability` and `actionEffectiveness` nullable unless backed by an auditable source/model + calibration metadata. `evidenceConfidence` is separately qualitative (`LOW|MEDIUM|HIGH|UNKNOWN`).

## Interface sketch (proposed, not frozen C#)

```csharp
public interface IWeatherReadPort {
    Task<ForecastWindowResult> GetWindowAsync(WeatherWindowQuery query, CancellationToken ct);
}
public interface IObservationReadPort {
    Task<ObservationWindowResult> QueryAsync(ObservationWindowQuery query, CancellationToken ct);
}
public interface IKnowledgeSearchPort {
    Task<EvidenceSearchResult> SearchAsync(EvidenceSearchRequest request, CancellationToken ct);
}
public interface IReasoningExecutor {
    Task<AgentAdviceResult> ExecuteAsync(AgentAdviceRequest request, CancellationToken ct);
}
public interface IDecisionReadPort {
    Task<DecisionPlanDto?> GetAsync(Guid workspaceId, Guid planId, CancellationToken ct);
}
```

Specific DTO/property names will be finalized in Gate 4 full LLD. Machine schemas here serve as interoperable design tests, **not** a claim all APIs exist or compile.

## Contract compatibility tests

`tests/Contracts`: JSON schema validity, examples for all contracts, old `v1` examples remain valid after additive updates, negative examples for missing workspace/date/subject, and security constraints forbidding inline copyrighted content in events. For future API OpenAPI, validate generated clients and HTTP status codes.
