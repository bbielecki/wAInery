using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.AIExecution.Contracts;

// Execution metrics only; predictions/ranking/reasoning request ports belong to Decisions.
public sealed record ModelInvocationDto(Guid RequestId, string Provider, string ExactModelVersion, string PromptVersion,
    long InputTokens, long OutputTokens, decimal EstimatedCostUsd, decimal? ActualBilledCostUsd,
    DateTimeOffset StartedAt, DateTimeOffset CompletedAt, string StatusCode, int Attempt, Guid CorrelationId);
public sealed record ModelUsageSummaryDto(WorkspaceId Workspace, TimeWindow Window, long InvocationCount,
    long InputTokens, long OutputTokens, decimal EstimatedCostUsd, decimal? ActualBilledCostUsd, DateTimeOffset KnownAt);
public sealed record ModelInvocationPageDto(WorkspaceId Workspace, TimeWindow Window,
    ImmutableArray<ModelInvocationDto> Items, string? NextCursor);
public interface IModelUsageReadPort
{
    Task<Result<ModelUsageSummaryDto>> GetUsageAsync(WorkspaceId workspace, TimeWindow window, CancellationToken ct);
    Task<Result<ModelInvocationPageDto>> GetInvocationsAsync(WorkspaceId workspace, TimeWindow window, int maxCount, string? cursor, CancellationToken ct);
}
