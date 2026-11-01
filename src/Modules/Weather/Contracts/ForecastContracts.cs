using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Weather.Contracts;

public enum ForecastIssueBasis { Unknown, Provider }
public enum ForecastFreshness { Fresh, Stale, Missing }

public sealed record ForecastPointDto(DateTimeOffset ValidAt, string VariableCode, decimal? Value, string Unit);

public sealed record ForecastRunDto
{
    public string RunId { get; }
    public WorkspaceId Workspace { get; }
    public string LocationId { get; }
    public SourceVersionRef Source { get; }
    public DateTimeOffset? IssuedAt { get; }
    public ForecastIssueBasis IssueBasis { get; }
    public DateTimeOffset RetrievedAt { get; }
    public IReadOnlyList<ForecastPointDto> Points { get; }

    public ForecastRunDto(string runId, WorkspaceId workspace, string locationId, SourceVersionRef source,
        DateTimeOffset? issuedAt, ForecastIssueBasis issueBasis, DateTimeOffset retrievedAt,
        IEnumerable<ForecastPointDto> points)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.SourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(source.Version);
        ArgumentNullException.ThrowIfNull(points);
        if (workspace.Value == Guid.Empty) throw new ArgumentException("Forecast workspace is required.");
        if (!((issueBasis == ForecastIssueBasis.Unknown && issuedAt is null)
            || (issueBasis == ForecastIssueBasis.Provider && issuedAt is not null)))
            throw new ArgumentException("Provider issue time must match its explicit basis.");
        var snapshot = points.Select(point => point is not null
            ? point with { ValidAt = point.ValidAt.ToUniversalTime() }
            : throw new ArgumentException("Forecast points cannot be null.")).ToArray();
        RunId = runId;
        Workspace = workspace;
        LocationId = locationId;
        Source = source;
        IssuedAt = issuedAt?.ToUniversalTime();
        IssueBasis = issueBasis;
        RetrievedAt = retrievedAt.ToUniversalTime();
        Points = Array.AsReadOnly(snapshot);
    }
}

public sealed record WeatherWindowDto(WorkspaceId Workspace, string LocationId, DateTimeOffset AsOf,
    TimeWindow Window, ForecastFreshness Freshness, ImmutableArray<ForecastRunDto> Runs,
    ImmutableArray<TimeWindow> MissingWindows);

public sealed record ForecastWindowChange(TimeWindow Window, ImmutableArray<string> VariableCodes);
public sealed record ForecastDiffDto(string PreviousRunId, string CurrentRunId, ImmutableArray<ForecastWindowChange> Changes);
public sealed record WeatherLocationDto(WorkspaceId Workspace, string LocationId, string Name,
    decimal Latitude, decimal Longitude, decimal? ElevationMetres, string TimeZoneId);
public sealed record RegisterWeatherLocationCommand(CommandContext Context, string LocationId, string Name,
    decimal Latitude, decimal Longitude, decimal? ElevationMetres, string TimeZoneId);
public sealed record ForecastPublishReceipt(string RunId, long AggregateVersion, DateTimeOffset RecordedAt, bool Duplicate);

public sealed record PublishForecastRunCommand
{
    public CommandContext Context { get; }
    public ForecastRunDto Run { get; }
    public string PayloadRef { get; }

    public PublishForecastRunCommand(CommandContext context, ForecastRunDto run, string payloadRef)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadRef);
        if (context.Workspace != run.Workspace) throw new ArgumentException("Forecast command workspace mismatch.");
        Context = context;
        Run = run;
        PayloadRef = payloadRef;
    }
}

public interface IWeatherReadPort
{
    Task<Result<ResolvedSubject>> ResolveAsync(SubjectRef location, CancellationToken ct);
    Task<Result<WeatherLocationDto>> GetLocationAsync(WorkspaceId workspace, string locationId, CancellationToken ct);
    Task<Result<WeatherWindowDto>> GetKnownForecastAsync(WorkspaceId workspace, string locationId,
        DateTimeOffset asOf, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<Result<ForecastDiffDto>> GetForecastDiffAsync(WorkspaceId workspace, string previousRunId,
        string currentRunId, TimeWindow window, CancellationToken ct);
}

public interface IWeatherCommandPort
{
    Task<Result<WeatherLocationDto>> RegisterLocationAsync(RegisterWeatherLocationCommand command, CancellationToken ct);
    Task<Result<ForecastPublishReceipt>> PublishForecastRunAsync(PublishForecastRunCommand command, CancellationToken ct);
}
