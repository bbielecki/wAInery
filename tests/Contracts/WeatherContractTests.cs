using wAInery.BuildingBlocks;
using wAInery.Modules.Weather.Contracts;

namespace wAInery.Contracts.Tests;

public sealed class WeatherContractTests
{
    private static readonly WorkspaceId Workspace = new(Guid.Parse("2c887032-d441-4cba-b9e0-d10a837dbb09"));
    private static readonly DateTimeOffset Retrieved = new(2026, 4, 18, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Published_contracts_live_in_the_owner_assembly()
    {
        Assert.Equal("wAInery.Modules.Weather.Contracts", typeof(IWeatherReadPort).Assembly.GetName().Name);
        Assert.Equal(typeof(IWeatherReadPort).Assembly, typeof(IWeatherCommandPort).Assembly);
    }

    [Fact]
    public void Unknown_issued_time_and_missing_numeric_value_stay_unknown()
    {
        var points = new List<ForecastPointDto> { new(Retrieved.AddHours(1), "air_temperature", null, "degC") };
        var run = new ForecastRunDto("forecast-1", Workspace, "location-A", new SourceVersionRef("weather-fixture", "1"),
            null, ForecastIssueBasis.Unknown, Retrieved, points);
        points.Clear();
        Assert.Null(run.IssuedAt);
        Assert.Null(Assert.Single(run.Points).Value);
        Assert.Equal("location-A", run.LocationId);
        Assert.Throws<NotSupportedException>(() => ((IList<ForecastPointDto>)run.Points).Clear());
    }

    [Fact]
    public void Issue_basis_must_match_the_presence_of_provider_time()
    {
        var source = new SourceVersionRef("weather-fixture", "1");
        Assert.Throws<ArgumentException>(() => new ForecastRunDto("run", Workspace, "location-A", source,
            null, ForecastIssueBasis.Provider, Retrieved, []));
        Assert.Throws<ArgumentException>(() => new ForecastRunDto("run", Workspace, "location-A", source,
            Retrieved, ForecastIssueBasis.Unknown, Retrieved, []));
    }

    [Fact]
    public void Command_context_requires_caller_scope_and_idempotency()
    {
        Assert.Throws<ArgumentException>(() => new CommandContext(default, "key", Guid.NewGuid(), "owner"));
        Assert.Throws<ArgumentException>(() => new CommandContext(Workspace, "", Guid.NewGuid(), "owner"));
        Assert.Throws<ArgumentException>(() => new CommandContext(Workspace, "key", Guid.Empty, "owner"));
        Assert.Throws<ArgumentException>(() => new CommandContext(Workspace, "key", Guid.NewGuid(), ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CommandContext(Workspace, "key", Guid.NewGuid(), "owner", -1));
        Assert.Throws<ArgumentException>(() => new CommandContext(Workspace, "key", Guid.NewGuid(), "owner", causationId: Guid.Empty));
    }

    [Fact]
    public void Publishing_cannot_cross_workspaces()
    {
        var run = new ForecastRunDto("run", Workspace, "location-A", new SourceVersionRef("fixture", "1"),
            null, ForecastIssueBasis.Unknown, Retrieved, []);
        var other = new CommandContext(new WorkspaceId(Guid.NewGuid()), "key", Guid.NewGuid(), "owner");
        Assert.Throws<ArgumentException>(() => new PublishForecastRunCommand(other, run, "receipt:1"));
        var matching = new CommandContext(Workspace, "key", Guid.NewGuid(), "owner", 0);
        Assert.Equal("receipt:1", new PublishForecastRunCommand(matching, run, "receipt:1").PayloadRef);
    }

    [Fact]
    public void Source_timestamps_normalize_to_UTC_without_changing_zero_to_unknown()
    {
        var sourceTime = Retrieved.ToOffset(TimeSpan.FromHours(2));
        var run = new ForecastRunDto("run", Workspace, "location-A", new SourceVersionRef("fixture", "1"),
            sourceTime, ForecastIssueBasis.Provider, sourceTime,
            [new ForecastPointDto(sourceTime.AddHours(1), "air_temperature", 0m, "degC")]);
        Assert.Equal(Retrieved, run.IssuedAt);
        Assert.Equal(TimeSpan.Zero, run.RetrievedAt.Offset);
        var point = Assert.Single(run.Points);
        Assert.Equal(TimeSpan.Zero, point.ValidAt.Offset);
        Assert.Equal(0m, point.Value);
    }
}
