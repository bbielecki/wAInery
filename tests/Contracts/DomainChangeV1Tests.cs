using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using wAInery.BuildingBlocks;

namespace wAInery.Contracts.Tests;

public sealed class DomainChangeV1Tests
{
    private static readonly string Root = FindRoot();
    private static readonly string Fixture = File.ReadAllText(Path.Combine(Root, "contracts/examples/domain-change.observation.json"));
    private static readonly JsonSchema Schema = JsonSchema.Build(JsonSerializer.Deserialize<JsonElement>(
        File.ReadAllText(Path.Combine(Root, "contracts/events/domain-change.v1.schema.json"))));

    [Fact]
    public void Existing_producer_fixture_roundtrips_through_scoped_consumer_and_wire_schema()
    {
        var change = DomainChangeV1Json.Deserialize(Fixture);
        Assert.Equal("wb-2026-001", change.Subject.Id);
        Assert.Equal("obs-0001", change.AggregateId);
        Assert.Equal(change.Workspace, change.Subject.Workspace);
        Assert.Equal(TimeSpan.Zero, change.RecordedAt.Offset);

        var json = DomainChangeV1Json.Serialize(change);
        Assert.True(Schema.Evaluate(JsonSerializer.Deserialize<JsonElement>(json)).IsValid);
        var consumed = DomainChangeV1Json.Deserialize(json);
        Assert.Equal(change.EventId, consumed.EventId);
        Assert.Equal(change.Subject, consumed.Subject);
        Assert.Equal(change.RecordedAt, consumed.RecordedAt);
        Assert.Equal(change.ChangedFields, consumed.ChangedFields);
    }

    [Theory]
    [InlineData("eventId", "\"not-a-uuid\"")]
    [InlineData("eventType", "\"UnknownEvent\"")]
    [InlineData("workspaceId", "\"00000000-0000-0000-0000-000000000000\"")]
    [InlineData("aggregateId", "\"\"")]
    [InlineData("aggregateVersion", "0")]
    [InlineData("schemaVersion", "2")]
    [InlineData("changedFields", "[]")]
    [InlineData("changedFields", "[\"x\",\"x\"]")]
    [InlineData("changedFields", "[\"\"]")]
    [InlineData("recordedAt", "\"not-a-date\"")]
    [InlineData("recordedAt", "\"2026-10-31T10:00:00\"")]
    [InlineData("recordedAt", "\"2026-10-31T10:00Z\"")]
    [InlineData("dataRef", "null")]
    [InlineData("subject", "{\"module\":\"Unknown\",\"kind\":\"Batch\",\"id\":\"x\"}")]
    public void Invalid_envelope_is_rejected(string field, string value)
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input[field] = JsonNode.Parse(value);
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
    }

    [Fact]
    public void Every_schema_required_field_is_required_by_the_consumer()
    {
        var schema = JsonNode.Parse(File.ReadAllText(Path.Combine(Root, "contracts/events/domain-change.v1.schema.json")))!;
        foreach (var field in schema["required"]!.AsArray())
        {
            var input = JsonNode.Parse(Fixture)!.AsObject();
            input.Remove(field!.GetValue<string>());
            Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
        }
    }

    [Theory]
    [InlineData("causationId")]
    [InlineData("sourceVersion")]
    [InlineData("timeRange")]
    public void Optional_fields_cannot_be_explicit_null_in_v1(string field)
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input[field] = null;
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
    }

    [Theory]
    [InlineData("checksum")]
    [InlineData("classification")]
    public void Optional_source_metadata_cannot_be_explicit_null(string field)
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input["sourceVersion"] = JsonNode.Parse("{\"sourceId\":\"manual-reading\",\"version\":\"1\"}");
        input["sourceVersion"]![field] = null;
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
    }

    [Theory]
    [InlineData("2026-10-31T12:00:04+02:00")]
    [InlineData("2026-10-31t10:00:04z")]
    public void Explicit_offset_and_RFC3339_case_variants_normalize_to_UTC(string timestamp)
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input["recordedAt"] = timestamp;
        var change = DomainChangeV1Json.Deserialize(input.ToJsonString());
        Assert.Equal(new DateTimeOffset(2026, 10, 31, 10, 0, 4, TimeSpan.Zero), change.RecordedAt);
        Assert.Equal(TimeSpan.Zero, change.RecordedAt.Offset);
    }

    [Fact]
    public void Unknown_text_fields_are_rejected_at_envelope_and_subject_boundaries()
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input["rawBookText"] = "synthetic forbidden field";
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
        input.Remove("rawBookText");
        input["subject"]!["workspaceId"] = Guid.NewGuid().ToString();
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
    }

    [Fact]
    public void Producer_cannot_emit_a_subject_from_another_workspace()
    {
        var input = DomainChangeV1Json.Deserialize(Fixture);
        var invalid = input with { Workspace = new WorkspaceId(Guid.NewGuid()) };
        Assert.Throws<ArgumentException>(() => DomainChangeV1Json.Serialize(invalid));
    }

    [Fact]
    public void Source_version_and_effective_window_survive_the_wire_boundary()
    {
        var input = JsonNode.Parse(Fixture)!.AsObject();
        input["sourceVersion"] = JsonNode.Parse("{\"sourceId\":\"manual-reading\",\"version\":\"v1\",\"classification\":\"PRIVATE_OPERATIONAL\"}");
        input["timeRange"] = JsonNode.Parse("{\"from\":\"2026-10-31T10:00:00Z\",\"to\":\"2026-10-31T11:00:00Z\"}");
        var change = DomainChangeV1Json.Deserialize(input.ToJsonString());
        Assert.Equal("manual-reading", change.SourceVersion!.SourceId);
        Assert.NotNull(change.TimeRange);
        Assert.True(Schema.Evaluate(JsonSerializer.Deserialize<JsonElement>(DomainChangeV1Json.Serialize(change))).IsValid);
        input["timeRange"]!["to"] = "2026-10-31T09:00:00Z";
        Assert.Throws<JsonException>(() => DomainChangeV1Json.Deserialize(input.ToJsonString()));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "wAInery.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Solution root not found.");
    }
}
