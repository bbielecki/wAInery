# LLD — Public ports, DTOs, contract governance

## C# reference signatures (interfaces, not precompiled code)

```csharp
public readonly record struct WorkspaceId(Guid Value);
public sealed record SubjectRef(WorkspaceId Workspace, string Module, string Kind, string Id);
public sealed record DataVersion(string SourceId, string Version, DateTimeOffset KnownAt);
public sealed record DomainChange(Guid EventId, int SchemaVersion, WorkspaceId Workspace,
    SubjectRef Subject, string EventType, IReadOnlySet<string> ChangedFields,
    long AggregateVersion, DateTimeOffset RecordedAt, string DataRef,
    Guid CorrelationId, Guid? CausationId);
public sealed record DecisionScope(WorkspaceId Workspace, SubjectRef Subject,
    string PredictionCode, DateTimeOffset HorizonStart, DateTimeOffset HorizonEnd);
public interface IVineyardReadPort { Task<BlockSnapshot?> GetBlockAsync(SubjectRef block, DateTimeOffset asOf, CancellationToken ct); }
public interface IWineryReadPort { Task<BatchSnapshot?> GetBatchAsync(SubjectRef batch, DateTimeOffset asOf, CancellationToken ct); }
public interface IObservationReadPort { Task<IReadOnlyList<ObservationDto>> QueryAsync(SubjectRef subject, string[] types, DateTimeOffset asOf, int max, CancellationToken ct); }
public interface IWeatherReadPort { Task<WeatherWindowDto> GetKnownForecastAsync(Guid locationId, DateTimeOffset asOf, DateTimeOffset from, DateTimeOffset to, CancellationToken ct); }
public interface IKnowledgeRetriever { Task<EvidencePage> RetrieveAsync(KnowledgeQuery query, CancellationToken ct); }
public interface IImpactAnalyzer { Task<IReadOnlyList<DecisionScope>> AffectedAsync(DomainChange change, CancellationToken ct); }
public interface IReasoningExecutor { Task<AgentAdviceResult> AdviseAsync(AgentAdviceRequest request, CancellationToken ct); }
public interface IDecisionReadPort { Task<DecisionPlanDto?> GetLatestAsync(SubjectRef subject, CancellationToken ct); }
public interface IObjectStorage { Task<ObjectInfo> PutAsync(Stream data, string classification, CancellationToken ct); Task<Stream> OpenReadAsync(string key, CancellationToken ct); }
public interface INotificationChannel { Task<DeliveryResult> SendAsync(NotificationMessage message, CancellationToken ct); } // Future phase only
```

**Identity rule:** public `SubjectRef.Id` and `SourceId` in v1 JSON contracts are stable opaque strings (e.g. `wb-2026-001`, `manual-reading`). PostgreSQL stores internal UUIDs. Each owning module exposes a resolution port mapping `(workspace, kind, publicId)` to canonical internal UUID, validates existence/ownership, and never guesses by name. External IDs cannot be reused for a different object. Registry does the equivalent for `SourceId`. `aggregateId` in event v1 is likewise an opaque stable string. **Do not break existing v1 fixtures by silently enforcing UUID on string IDs.**

Owner aggregate types are **not** shared; public ports return immutable DTOs from module `Contracts` project. Cross-module runtime calls go through ports, not EF entities, storage schemas, or internal namespaces.

## Contracts and envelope
Machine schemas in `contracts/events`, `contracts/tools`, `contracts/api`; samples in `contracts/examples`. Schema v1 requires UUID event and correlation IDs, type, subject, source version, aggregate version, changed fields and `dataRef`. Event payload may contain safe metadata but never raw copyrighted paragraphs/prompts. Any added JSON required field, enum-value removal, rename, unit semantics or time semantics is **breaking**: new version + compatibility fixture. Minor optional fields may be additive only if all readers tolerate unknown fields or explicitly negotiate contract version. Keep versioned schemas side by side during migration.

Commands carry caller scope and idempotency key. Result errors `Validation`, `NotFound`, `Conflict`, `PolicyDenied`, `Unavailable`, `RateLimited`, `BudgetExceeded`, `RetryableFailure` with `retryable` and correlation ID; never raw provider secrets. Reject subject ownership mismatch and source workspace mismatch. Validate record units against Measurement Registry; quantity is decimal + explicit unit and method; °Brix, acidity g/L, and pH are distinct.

## Scope ownership & parallel agents
- Integration owner: `src/BuildingBlocks`, `contracts/**`, `src/Host`, migration bootstrap, `tests/Contracts`.
- Domain agent: only its `src/Modules/<Name>/**` and `tests/Modules/<Name>/**`; propose shared changes in `agents/handoffs/<Name>-contract-change.md`.
- Knowledge agent owns IKnowledgeRetriever implementation but not ModelGateway. Decisions agent owns IImpactAnalyzer and decision plan. AIExecution agent owns IReasoningExecutor implementation, policies and usage metrics.
- Never silently change another module's `Contract` assembly; contract freeze is a distinct integration PR with fixtures.

## Provider boundary
`IModelGateway` accepts capability requirements (`structured_output`, tool calls, max token count, allowed data classes, cost ceiling) instead of vendor model names; provider response includes model exact version, token counts, estimated and actual billing where available. API versions of models and prompts are pinned and evaluated before default switch. Embedding model space version/normalizer/dimensions persisted; dual-index rebuild when replacing embeddings.
