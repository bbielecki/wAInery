using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using wAInery.BuildingBlocks;
using wAInery.Modules.Decisions.Contracts;

namespace wAInery.Contracts.Tests;

// G1-04 / #30: constructor/compatibility evidence only, not DB catch-up/CAS,
// model execution, egress authorization or scientific probability validation.
public sealed class DecisionContractsTests
{
    private static readonly WorkspaceId Workspace = new(Guid.Parse("2c887032-d441-4cba-b9e0-d10a837dbb09"));
    private static readonly SubjectRef Block = new(Workspace, "Vineyard", "Block", "synthetic-block-A");
    private static readonly DateTimeOffset AsOf = DateTimeOffset.Parse("2026-10-31T11:15:00+01:00");
    private static readonly Guid RequestId = Guid.Parse("e25e7962-27c9-4bd3-a03d-e37cf7df38a1");

    [Fact]
    public void Facts_keep_null_zero_false_string_and_decimal_after_original_documents_are_disposed()
    {
        foreach (var json in new[] { "null", "0", "false", "\"synthetic\"", "16.4" })
        {
            var fact = Fact(json);
            Assert.Equal(json, fact.Value.GetRawText());
        }
        Assert.Equal(JsonValueKind.Null, Fact("null").Value.ValueKind);
        Assert.Equal(0m, Fact("0").Value.GetDecimal());
        Assert.False(Fact("false").Value.GetBoolean());
        Assert.Equal(16.4m, Fact("16.4").Value.GetDecimal());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    public void Fact_rejects_non_scalar_values(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.ThrowsAny<ArgumentException>(() => new AdviceFact("fixture", document.RootElement, "synthetic-source"));
    }

    [Fact]
    public void Undefined_fact_and_unknown_classification_are_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => new AdviceFact("fixture", default, "synthetic-source"));
        Assert.ThrowsAny<ArgumentException>(() => Fact("0", "UNREVIEWED_PUBLIC"));
        Assert.ThrowsAny<ArgumentException>(() => Request(sourceVersions:
            [new SourceVersionRef("synthetic-source", "1", Classification: "UNREVIEWED_PUBLIC")]));
    }

    [Fact]
    public void Existing_classifications_and_absent_unknown_metadata_remain_constructible()
    {
        // Acceptance of classification metadata is not an external egress grant.
        foreach (var classification in new string?[]
            { null, "PRIVATE_COPYRIGHT", "PRIVATE_DERIVED", "PRIVATE_OPERATIONAL", "PUBLIC_LICENSED", "PUBLIC_REFERENCE" })
        {
            var request = Request(sourceVersions: [new SourceVersionRef("synthetic-source", "1", Classification: classification)],
                facts: [Fact("0", classification)]);
            Assert.Equal(classification, Assert.Single(request.SourceVersions).Classification);
            Assert.Equal(classification, Assert.Single(request.Facts).Classification);
        }
    }

    [Fact]
    public void Request_snapshots_mutable_provenance_facts_evidence_and_tools()
    {
        var sources = new[] { new SourceVersionRef("synthetic-source", "1") };
        var evidence = new[] { "synthetic-evidence" };
        var facts = new List<AdviceFact> { Fact("0") };
        var tools = new[] { AdviceTool.GET_WEATHER };
        var request = Request(sources, evidence, facts, tools);
        sources[0] = new("changed", "2");
        evidence[0] = "changed";
        facts.Clear();
        tools[0] = AdviceTool.GET_OBSERVATIONS;
        Assert.Equal("synthetic-source", Assert.Single(request.SourceVersions).SourceId);
        Assert.Equal("synthetic-evidence", Assert.Single(request.EvidenceRefs));
        Assert.Equal(0m, Assert.Single(request.Facts).Value.GetDecimal());
        Assert.Equal(AdviceTool.GET_WEATHER, Assert.Single(request.AllowedTools));
        Assert.Equal(TimeSpan.Zero, request.AsOf.Offset);
        Assert.Equal(AsOf.ToUniversalTime(), request.AsOf);
    }

    [Fact]
    public void Request_accepts_technical_boundaries_and_true_zero_cost_ceiling()
    {
        var request = Request(
            Enumerable.Range(0, 512).Select(i => new SourceVersionRef("synthetic-" + i, "1")),
            Enumerable.Range(0, 256).Select(i => "evidence-" + i),
            Enumerable.Range(0, 512).Select(i => Fact("0", key: "synthetic-metric-" + i)),
            [AdviceTool.SEARCH_EVIDENCE, AdviceTool.GET_OBSERVATIONS, AdviceTool.GET_WEATHER, AdviceTool.GET_DOMAIN_CONTEXT],
            purpose: AdvicePurpose.EVALUATION);
        Assert.Equal(AdvicePurpose.EVALUATION, request.Purpose);
        Assert.Equal(512, request.SourceVersions.Length);
        Assert.Equal(512, request.Facts.Length);
        Assert.Equal(256, request.EvidenceRefs.Length);
        Assert.Equal(4, request.AllowedTools.Length);
        Assert.Equal(1, request.MaxInputTokens);
        Assert.Equal(1, request.MaxOutputTokens);
        Assert.Equal(0m, request.MaxCostUsd);
        Assert.Equal(1000, request.TimeoutMs);
    }

    [Fact]
    public void Request_rejects_oversized_context_duplicate_or_unknown_tools_and_undefined_roles()
    {
        Assert.ThrowsAny<ArgumentException>(() => Request(sourceVersions:
            Enumerable.Range(0, 513).Select(i => new SourceVersionRef("synthetic-" + i, "1"))));
        Assert.ThrowsAny<ArgumentException>(() => Request(facts: Enumerable.Range(0, 513).Select(i => Fact("0", key: "synthetic-metric-" + i))));
        Assert.ThrowsAny<ArgumentException>(() => Request(evidence: Enumerable.Range(0, 257).Select(i => "evidence-" + i)));
        Assert.ThrowsAny<ArgumentException>(() => Request(tools: [AdviceTool.GET_WEATHER, AdviceTool.GET_WEATHER]));
        Assert.ThrowsAny<ArgumentException>(() => Request(tools: [(AdviceTool)999]));
        Assert.ThrowsAny<ArgumentException>(() => Request(purpose: (AdvicePurpose)999));
        Assert.ThrowsAny<ArgumentException>(() => Request(role: (AdvisorRole)999));
    }

    [Theory]
    [InlineData(0, 1, "0", 1000)]
    [InlineData(1, 0, "0", 1000)]
    [InlineData(1, 1, "-0.01", 1000)]
    [InlineData(1, 1, "0", 999)]
    public void Request_rejects_budgets_below_v1_minima(int input, int output, string cost, int timeout)
        => Assert.ThrowsAny<ArgumentException>(() => Request(input: input, output: output,
            cost: decimal.Parse(cost, System.Globalization.CultureInfo.InvariantCulture), timeout: timeout));

    [Fact]
    public void Request_rejects_missing_identity_invalid_subject_and_cross_workspace_scope()
    {
        Assert.ThrowsAny<ArgumentException>(() => Request(requestId: Guid.Empty));
        Assert.ThrowsAny<ArgumentException>(() => Request(workspace: new(Guid.Empty)));
        Assert.ThrowsAny<ArgumentException>(() => Request(subject: Block with { Workspace = new(Guid.NewGuid()) }));
        Assert.ThrowsAny<ArgumentException>(() => Request(subject: Block with { Id = " " }));
        Assert.ThrowsAny<ArgumentException>(() => Request(inputRevision: 0));
    }

    [Fact]
    public void Scope_preserves_definition_and_opaque_id_but_rejects_invalid_or_default_window()
    {
        var horizon = new TimeWindow(AsOf, AsOf.AddHours(24));
        var scope = new DecisionScope(Workspace, Block, "frost_exposure", "fixture-v1", horizon);
        Assert.Equal("synthetic-block-A", scope.Subject.Id);
        Assert.Equal("fixture-v1", scope.DefinitionVersion);
        Assert.Equal(TimeSpan.Zero, scope.Horizon.From.Offset);
        Assert.Equal(TimeSpan.Zero, scope.Horizon.To.Offset);
        Assert.ThrowsAny<ArgumentException>(() => new DecisionScope(new(Guid.Empty), Block, "frost_exposure", "fixture-v1", horizon));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionScope(new(Guid.NewGuid()), Block, "frost_exposure", "fixture-v1", horizon));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionScope(Workspace, Block, " ", "fixture-v1", horizon));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionScope(Workspace, Block, "frost_exposure", " ", horizon));
        Assert.ThrowsAny<ArgumentException>(() => new DecisionScope(Workspace, Block, "frost_exposure", "fixture-v1", default));
    }

    [Fact]
    public void Probability_structural_guards_preserve_zero_and_all_existing_calibration_labels()
    {
        // Synthetic metadata does not establish scientific validity.
        foreach (var calibration in new[]
            { CalibrationStatus.VALIDATED, CalibrationStatus.UNVALIDATED, CalibrationStatus.EXPERT_ESTIMATE })
        {
            var probability = Probability(0m, calibration);
            Assert.Equal(0m, probability.Value);
            Assert.Equal(calibration, probability.CalibrationStatus);
        }
        Assert.Equal(1m, Probability(1m).Value);
        Assert.ThrowsAny<ArgumentException>(() => Probability(-0.01m));
        Assert.ThrowsAny<ArgumentException>(() => Probability(1.01m));
        Assert.ThrowsAny<ArgumentException>(() => Probability(0.5m, (CalibrationStatus)999));
        Assert.ThrowsAny<ArgumentException>(() => new ProbabilityDto(0m, " ", "fixture-v1", CalibrationStatus.UNVALIDATED, "synthetic basis"));
        Assert.ThrowsAny<ArgumentException>(() => new ProbabilityDto(0m, "fixture", " ", CalibrationStatus.UNVALIDATED, "synthetic basis"));
        Assert.ThrowsAny<ArgumentException>(() => new ProbabilityDto(0m, "fixture", "fixture-v1", CalibrationStatus.UNVALIDATED, " "));
    }

    [Fact]
    public void Current_freshness_requires_all_catchup_revision_definition_and_expiry_conditions()
    {
        var current = new DecisionFreshnessDto(ReadFreshness.CURRENT, AsOf, true, false, AsOf, 2, 2, true, AsOf.AddMinutes(1));
        Assert.Equal(ReadFreshness.CURRENT, current.Status);
        Assert.Equal(TimeSpan.Zero, current.CheckedAt.Offset);
        foreach (var invalid in new[] { "catchup", "pending", "definition", "revision", "unknown-revision", "unknown-expiry", "expired", "expiry-boundary" })
        {
            long? published = invalid == "unknown-revision" ? null : invalid == "revision" ? 1L : 2L;
            DateTimeOffset? until = invalid switch
            {
                "unknown-expiry" => null,
                "expired" => AsOf.AddSeconds(-1),
                "expiry-boundary" => AsOf,
                _ => AsOf.AddMinutes(1)
            };
            Assert.ThrowsAny<ArgumentException>(() => new DecisionFreshnessDto(ReadFreshness.CURRENT, AsOf,
                invalid != "catchup", invalid == "pending", AsOf, 2, published, invalid != "definition", until));
        }
        var pending = new DecisionFreshnessDto(ReadFreshness.PENDING_REEVALUATION, AsOf,
            false, true, null, 2, 1, false, null);
        Assert.Equal(ReadFreshness.PENDING_REEVALUATION, pending.Status);
        Assert.Null(pending.ValidUntil);
        Assert.False(pending.ImpactCaughtUp);
    }

    [Fact]
    public void Existing_wire_does_not_accept_pending_plan_state_or_llm_success_probability()
    {
        var plan = JsonNode.Parse(File.ReadAllText(RepoFile("contracts/examples/decision-plan.json")))!;
        Assert.True(SchemaAccepts("contracts/api/decision-plan.v1.schema.json", plan));
        plan["state"] = "PENDING_REEVALUATION";
        Assert.False(SchemaAccepts("contracts/api/decision-plan.v1.schema.json", plan));
        var advice = JsonNode.Parse(File.ReadAllText(RepoFile("contracts/examples/agent-advice-result.json")))!;
        Assert.True(SchemaAccepts("contracts/tools/agent-advice-result.v1.schema.json", advice));
        advice["actionCandidates"]![0]!["predictedSuccessProbability"] = 0.8m;
        Assert.False(SchemaAccepts("contracts/tools/agent-advice-result.v1.schema.json", advice));
    }

    [Fact]
    public void Existing_request_fixture_can_be_manually_consumed_with_opaque_refs_and_owned_fact_values()
    {
        // Explicit fixture mapping only: no production HTTP decoder is claimed.
        AgentAdviceRequest request;
        using (var document = JsonDocument.Parse(File.ReadAllText(RepoFile("contracts/examples/agent-advice-request.json"))))
        {
            var wire = document.RootElement;
            var subject = wire.GetProperty("subject");
            var workspace = new WorkspaceId(wire.GetProperty("workspaceId").GetGuid());
            request = new(wire.GetProperty("requestId").GetGuid(), workspace,
                Enum.Parse<AdvicePurpose>(wire.GetProperty("purpose").GetString()!),
                Enum.Parse<AdvisorRole>(wire.GetProperty("role").GetString()!),
                new(workspace, subject.GetProperty("module").GetString()!, subject.GetProperty("kind").GetString()!, subject.GetProperty("id").GetString()!),
                wire.GetProperty("asOf").GetDateTimeOffset(), wire.GetProperty("inputRevision").GetInt64(),
                wire.GetProperty("sourceVersions").EnumerateArray().Select(source => new SourceVersionRef(
                    source.GetProperty("sourceId").GetString()!, source.GetProperty("version").GetString()!,
                    Optional(source, "checksum"), Optional(source, "classification"))),
                wire.GetProperty("evidenceRefs").EnumerateArray().Select(value => value.GetString()!),
                wire.GetProperty("facts").EnumerateArray().Select(fact => new AdviceFact(
                    fact.GetProperty("key").GetString()!, fact.GetProperty("value"), fact.GetProperty("sourceRef").GetString()!,
                    Optional(fact, "unit"), Optional(fact, "classification"))),
                wire.GetProperty("allowedTools").EnumerateArray().Select(value => Enum.Parse<AdviceTool>(value.GetString()!)),
                wire.GetProperty("maxInputTokens").GetInt32(), wire.GetProperty("maxOutputTokens").GetInt32(),
                wire.GetProperty("maxCostUsd").GetDecimal(), wire.GetProperty("timeoutMs").GetInt32());
            Assert.Equal(wire.GetProperty("outputSchemaId").GetString(), request.OutputSchemaId);
        }
        Assert.Equal(1, request.SchemaVersion);
        Assert.Equal(AdvicePurpose.PRIVATE_ADVICE, request.Purpose);
        Assert.Equal(AdvisorRole.WINEMAKING_ADVISOR, request.Role);
        Assert.Equal("wb-2026-001", request.Subject.Id);
        Assert.Equal("observations:obs-0001", Assert.Single(request.SourceVersions).SourceId);
        Assert.Equal(16.4m, Assert.Single(request.Facts).Value.GetDecimal());
        Assert.Equal(AdviceTool.GET_OBSERVATIONS, Assert.Single(request.AllowedTools));
        Assert.Equal(0.01m, request.MaxCostUsd);
    }

    private static AdviceFact Fact(string json, string? classification = "PRIVATE_OPERATIONAL", string key = "wine_temperature")
    {
        using var document = JsonDocument.Parse(json);
        return new(key, document.RootElement, "synthetic-observation", "Cel", classification);
    }

    private static AgentAdviceRequest Request(IEnumerable<SourceVersionRef>? sourceVersions = null,
        IEnumerable<string>? evidence = null, IEnumerable<AdviceFact>? facts = null, IEnumerable<AdviceTool>? tools = null,
        int input = 1, int output = 1, decimal cost = 0m, int timeout = 1000,
        Guid? requestId = null, WorkspaceId? workspace = null, SubjectRef? subject = null,
        AdvicePurpose purpose = AdvicePurpose.PRIVATE_ADVICE, AdvisorRole role = AdvisorRole.VINEYARD_ADVISOR,
        long inputRevision = 1)
        => new(requestId ?? RequestId, workspace ?? Workspace, purpose, role, subject ?? Block, AsOf, inputRevision,
            sourceVersions ?? [], evidence ?? [], facts ?? [], tools ?? [], input, output, cost, timeout);

    private static ProbabilityDto Probability(decimal value, CalibrationStatus calibration = CalibrationStatus.UNVALIDATED)
        => new(value, "synthetic-method", "fixture-v1", calibration, "generated test basis");

    private static string? Optional(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) ? value.GetString() : null;

    private static bool SchemaAccepts(string relativePath, JsonNode instance)
    {
        using var schemaDocument = JsonDocument.Parse(File.ReadAllText(RepoFile(relativePath)));
        return JsonSchema.Build(schemaDocument.RootElement).Evaluate(JsonSerializer.SerializeToElement(instance)).IsValid;
    }

    private static string RepoFile(string path)
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "wAInery.slnx")))
            root = Directory.GetParent(root)?.FullName ?? throw new InvalidOperationException("Repository not found.");
        return Path.Combine(root, path);
    }
}
