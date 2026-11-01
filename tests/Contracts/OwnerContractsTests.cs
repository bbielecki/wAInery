using System.Text.Json;
using wAInery.BuildingBlocks;
using wAInery.Modules.Observations.Contracts;
using wAInery.Modules.Vineyard.Contracts;
using wAInery.Modules.Winery.Contracts;

namespace wAInery.Contracts.Tests;

public sealed class OwnerContractsTests
{
    private static readonly WorkspaceId Workspace = new(Guid.Parse("2c887032-d441-4cba-b9e0-d10a837dbb09"));
    private static CommandContext Context => new(Workspace, "fixture-1", Guid.NewGuid(), "synthetic-owner");
    private static SubjectRef Block => new(Workspace, "Vineyard", "Block", "block-A");
    private static SubjectRef Batch => new(Workspace, "Winery", "WineBatch", "wb-2026-001");

    [Fact]
    public void Scoped_commands_reject_cross_workspace_and_wrong_owner()
    {
        var foreign = Batch with { Workspace = new(Guid.NewGuid()) };
        Assert.Throws<ArgumentException>(() => new RecordObservationCommand(Context, foreign, "temperature",
            ObservationValue.Number(0, "Cel"), "manual-reading", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new RegisterBlockCommand(Context, Batch, "fixture"));
        Assert.Throws<ArgumentException>(() => new CreateWineBatchCommand(Context, Block, "fixture", "ACTIVE"));
        Assert.Throws<ArgumentException>(() => new UpdateBlockLocationCommand(Context, foreign, "pl-weather-A"));
        Assert.Throws<ArgumentException>(() => new PrepareCellarConfirmationCommand(Context, foreign, "draft-1", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Confirmation_requires_both_proposal_and_token()
    {
        Assert.Throws<ArgumentException>(() => new OperationConfirmation("", "token", DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() => new OperationConfirmation("draft-1", "", DateTimeOffset.UtcNow));
        var confirmation = new OperationConfirmation("draft-1", "synthetic-confirmation", DateTimeOffset.Parse("2026-10-31T12:00:00+02:00"));
        var command = new ConfirmCellarOperationCommand(Context, Batch, confirmation);
        Assert.Equal(TimeSpan.Zero, command.Confirmation.PerformedAt.Offset);
        Assert.Equal("draft-1", command.Confirmation.ProposalId);
        Assert.Throws<ArgumentNullException>(() => new ConfirmCellarOperationCommand(Context, Batch, null!));
        Assert.Throws<ArgumentNullException>(() => new ConfirmVineyardActivityCommand(Context, Block, null!));
    }

    [Fact]
    public void Observation_query_is_bounded_and_copies_its_type_filter()
    {
        var types = new[] { "wine_temperature" };
        var query = new ObservationQuery(Batch, types, DateTimeOffset.Parse("2026-10-31T12:00:00+02:00"), 50);
        types[0] = "changed-after-construction";
        Assert.Equal("wine_temperature", Assert.Single(query.Types));
        Assert.Equal(TimeSpan.Zero, query.AsOf.Offset);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObservationQuery(Batch, [], DateTimeOffset.UtcNow, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObservationQuery(Batch, [], DateTimeOffset.UtcNow, 501));
    }

    [Fact]
    public void Resolution_preserves_opaque_identity_and_rejects_missing_canonical_identity()
    {
        var resolved = new ResolvedSubject(Batch, Guid.NewGuid(), 1);
        Assert.Equal("wb-2026-001", resolved.Subject.Id);
        Assert.Throws<ArgumentException>(() => new ResolvedSubject(Batch, Guid.Empty, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ResolvedSubject(Batch, Guid.NewGuid(), 0));
    }

    [Fact]
    public void Observation_value_keeps_zero_and_false_distinct_from_missing()
    {
        var zero = ObservationValue.Number(0, "Cel", "fixture-method");
        var negative = ObservationValue.Number(-2.75m, "Cel");
        var no = ObservationValue.Boolean(false);
        Assert.Equal(0m, zero.NumericValue);
        Assert.Equal("fixture-method", zero.Method);
        Assert.Equal(-2.75m, negative.NumericValue);
        Assert.Null(no.NumericValue);
        Assert.False(no.BooleanValue);
        Assert.Null(zero.BooleanValue);
        Assert.Throws<ArgumentException>(() => ObservationValue.Number(0, ""));
        Assert.Throws<ArgumentException>(() => ObservationValue.Narrative(" "));
    }

    [Fact]
    public void Historic_numeric_fixture_can_be_consumed_without_converting_opaque_ids()
    {
        var rootPath = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(rootPath, "wAInery.slnx")))
            rootPath = Directory.GetParent(rootPath)?.FullName ?? throw new InvalidOperationException("Repository not found.");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(rootPath, "contracts/examples/record-observation.numeric.json")));
        var wire = document.RootElement;
        var subject = wire.GetProperty("subject");
        var context = new CommandContext(new(wire.GetProperty("workspaceId").GetGuid()), wire.GetProperty("idempotencyKey").GetString()!, Guid.NewGuid(), "synthetic-owner");
        var command = new RecordObservationCommand(context,
            new(context.Workspace, subject.GetProperty("module").GetString()!, subject.GetProperty("kind").GetString()!, subject.GetProperty("id").GetString()!),
            wire.GetProperty("observationType").GetString()!, ObservationValue.Number(wire.GetProperty("numericValue").GetDecimal(), wire.GetProperty("unit").GetString()!),
            wire.GetProperty("sourceId").GetString()!, wire.GetProperty("observedAt").GetDateTimeOffset(), wire.GetProperty("receivedAt").GetDateTimeOffset());
        Assert.Equal("wb-2026-001", command.Subject.Id);
        Assert.Equal("manual-reading", command.SourceId);
        Assert.Equal(16.4m, command.OriginalValue.NumericValue);
        Assert.Equal(IntakeQuality.Unchecked, command.Quality);
    }
}
