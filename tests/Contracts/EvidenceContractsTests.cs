using System.Collections.Immutable;
using wAInery.BuildingBlocks;
using wAInery.Modules.Knowledge.Contracts;
using wAInery.Modules.Connectivity.Contracts;

namespace wAInery.Contracts.Tests;

// Constructor/metadata contracts only. Current rights, storage and retrieval need
// real owner implementations; these tests neither grant rights nor fake handlers.
public sealed class EvidenceContractsTests
{
    private static readonly WorkspaceId Workspace = new(Guid.Parse("2c887032-d441-4cba-b9e0-d10a837dbb09"));
    private static readonly DateTimeOffset At = DateTimeOffset.Parse("2026-10-31T10:00:00Z");
    private const string EmptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
    private static EvidenceContext Context => new(
        new SubjectRef(Workspace, "Vineyard", "Block", "block-fixture-A"),
        ImmutableArray.Create("frost"), ImmutableArray.Create("cultivar-fixture"),
        [], [], "PL", "region-fixture");

    [Fact]
    public void A_protected_source_snapshot_defaults_to_deny()
    {
        var snapshot = new SourceRightsSnapshot(Workspace, "source-fixture-A", 1,
            SourceRightsClass.PrivateCopyright, At);
        Assert.Equal(ExternalAiPolicy.Deny, snapshot.Policy);
        Assert.Equal(SourceRightsClass.PrivateCopyright, snapshot.RightsClass);
        Assert.Null(snapshot.AuthorizationRef);
    }

    [Fact]
    public void Explicit_allow_requires_provenance_and_keeps_opaque_source_identity()
    {
        var snapshot = new SourceRightsSnapshot(Workspace, "source-fixture-A", 7,
            SourceRightsClass.PrivateCopyright, At, ExternalAiPolicy.Allow, "approval-fixture-7");
        Assert.Equal("source-fixture-A", snapshot.SourceId);
        Assert.Equal(Workspace, snapshot.Workspace);
        Assert.Equal(7, snapshot.RegistryVersion);
        Assert.Equal(At, snapshot.CheckedAt);
        Assert.Equal(ExternalAiPolicy.Allow, snapshot.Policy);
        Assert.Equal("approval-fixture-7", snapshot.AuthorizationRef);
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 7,
            SourceRightsClass.PrivateCopyright, At, ExternalAiPolicy.Allow));
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 7,
            SourceRightsClass.PrivateCopyright, At, ExternalAiPolicy.Allow, " "));
    }

    [Fact]
    public void Unknown_class_is_preserved_without_becoming_an_allow_grant()
    {
        var unknown = new SourceRightsSnapshot(Workspace, "source-fixture-A", 1, SourceRightsClass.Unknown, At);
        var review = new SourceRightsSnapshot(Workspace, "source-fixture-A", 1,
            SourceRightsClass.PrivateDerived, At, ExternalAiPolicy.Review);
        Assert.Equal(SourceRightsClass.Unknown, unknown.RightsClass);
        Assert.Equal(ExternalAiPolicy.Deny, unknown.Policy);
        Assert.Equal(ExternalAiPolicy.Review, review.Policy);
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 1,
            SourceRightsClass.Unknown, At, ExternalAiPolicy.Allow, "approval-fixture-1"));
    }

    [Fact]
    public void A_source_snapshot_rejects_invalid_identity_version_and_enum_metadata()
    {
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(default, "source-fixture-A", 1, SourceRightsClass.PublicReference, At));
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, " ", 1, SourceRightsClass.PublicReference, At));
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 0, SourceRightsClass.PublicReference, At));
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 1, (SourceRightsClass)999, At));
        Assert.ThrowsAny<ArgumentException>(() => new SourceRightsSnapshot(Workspace, "source-fixture-A", 1, SourceRightsClass.PublicReference, At, (ExternalAiPolicy)999));
    }

    [Fact]
    public void An_existing_zero_byte_object_can_have_unknown_classification()
    {
        var stored = new StoredObjectRef(Workspace, "object-fixture-empty", EmptySha256, 0,
            "application/octet-stream", SourceRightsClass.Unknown);
        Assert.Equal(Workspace, stored.Workspace);
        Assert.Equal("object-fixture-empty", stored.Key);
        Assert.Equal(EmptySha256, stored.Sha256);
        Assert.Equal(0, stored.LengthBytes);
        Assert.Equal("application/octet-stream", stored.MediaType);
        Assert.Equal(SourceRightsClass.Unknown, stored.Classification);
        // A descriptor with known length0 is not a storage NOT_FOUND response.
        // No live object or checksum verification is implied by constructing it.
    }

    [Fact]
    public void Object_hash_metadata_requires_exact_lowercase_sha256_shape()
    {
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", EmptySha256.ToUpperInvariant(), 0,
            "application/pdf", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", EmptySha256[..63], 0,
            "application/pdf", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", new string('z', 64), 0,
            "application/pdf", SourceRightsClass.PrivateCopyright));
    }

    [Fact]
    public void Object_metadata_rejects_invalid_scope_length_labels_and_classification()
    {
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(default, "object-fixture-A", EmptySha256, 0,
            "application/pdf", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", EmptySha256, -1,
            "application/pdf", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, " ", EmptySha256, 0,
            "application/pdf", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", EmptySha256, 0,
            " ", SourceRightsClass.PrivateCopyright));
        Assert.ThrowsAny<ArgumentException>(() => new StoredObjectRef(Workspace, "object-fixture-A", EmptySha256, 0,
            "application/pdf", (SourceRightsClass)999));
    }

    [Fact]
    public void A_local_query_keeps_context_and_upper_bounds_with_utc_as_of()
    {
        var query = new KnowledgeQuery(Workspace, Context, "synthetic frost question", EvidencePurpose.LocalOwnerAnalysis,
            DateTimeOffset.Parse("2026-10-31T12:00:00+02:00"), 50, 16384);
        Assert.Equal(Workspace, query.Workspace);
        Assert.Equal("block-fixture-A", query.Context.Subject.Id);
        Assert.Equal("frost", Assert.Single(query.Context.TopicTags));
        Assert.Equal("PL", query.Context.CountryCode);
        Assert.Equal("synthetic frost question", query.QueryText);
        Assert.Equal(EvidencePurpose.LocalOwnerAnalysis, query.Purpose);
        Assert.Equal(At, query.AsOf);
        Assert.Equal(TimeSpan.Zero, query.AsOf.Offset);
        Assert.Equal(50, query.TopK);
        Assert.Equal(16384, query.TokenBudget);
        Assert.Null(query.TargetModelProfileId);
        Assert.Null(query.Cursor);
    }

    [Fact]
    public void A_query_rejects_foreign_or_invalid_subject_scope()
    {
        var foreign = Context with { Subject = Context.Subject with { Workspace = new(Guid.Parse("2c887032-d441-4cba-b9e0-d10a837dbb10")) } };
        var invalid = Context with { Subject = Context.Subject with { Id = " " } };
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, foreign, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 5, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, invalid, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 5, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(default, Context, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 5, 1024));
    }

    [Fact]
    public void A_query_cannot_request_zero_or_excessive_evidence_budgets()
    {
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 0, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 51, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 5, 0));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.LocalOwnerAnalysis, At, 5, 16385));
    }

    [Fact]
    public void External_context_requires_an_explicit_target_profile()
    {
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.ExternalModelContext, At, 5, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.ExternalModelContext, At, 5, 1024, " "));
        var request = new KnowledgeQuery(Workspace, Context, "fixture", EvidencePurpose.ExternalModelContext, At, 5, 1024, "profile-fixture");
        Assert.Equal("profile-fixture", request.TargetModelProfileId);
        // A valid request DTO is not authorization to call that profile.
    }

    [Fact]
    public void A_query_rejects_undefined_purpose_and_blank_query_text()
    {
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, "fixture", (EvidencePurpose)999, At, 5, 1024));
        Assert.ThrowsAny<ArgumentException>(() => new KnowledgeQuery(Workspace, Context, " ", EvidencePurpose.LocalOwnerAnalysis, At, 5, 1024));
    }

    [Fact]
    public void Registration_defaults_to_deny_and_receipt_cannot_reference_a_foreign_object()
    {
        var context = new CommandContext(Workspace, "receipt-fixture", Guid.NewGuid(), "synthetic-owner");
        var source = new RegisterSourceCommand(context, "source-fixture-A", "MANUAL", "synthetic",
            "fixture", "1", "1", "config-fixture", SourceRightsClass.PublicReference, null);
        Assert.Equal(ExternalAiPolicy.Deny, source.InitialPolicy);
        var foreign = new StoredObjectRef(new(Guid.NewGuid()), "object-fixture", EmptySha256, 0,
            "application/octet-stream", SourceRightsClass.PrivateOperational);
        Assert.Throws<ArgumentException>(() => new CaptureReceiptCommand(context, source.SourceId, 1,
            "external-fixture", foreign, "1", "1"));
        var own = new StoredObjectRef(Workspace, "object-fixture", EmptySha256, 0,
            "application/octet-stream", SourceRightsClass.PrivateOperational);
        var capture = new CaptureReceiptCommand(context, source.SourceId, 1, "external-fixture", own, "1", "1");
        Assert.Equal("source-fixture-A", capture.SourceId);
        Assert.Equal("external-fixture", capture.ExternalKey);
    }

    [Fact]
    public void Document_import_rejects_foreign_object_without_inventing_missing_locality()
    {
        var context = new CommandContext(Workspace, "document-fixture", Guid.NewGuid(), "synthetic-owner");
        var metadata = new EvidenceMetadata("synthetic", null, null, null, At, "pl", null,
            [], [], [], [], null, [], null, null, [], PublicationState.Unknown, TagCoverage.Unknown, EmptySha256);
        var foreign = new StoredObjectRef(new(Guid.NewGuid()), "object-fixture", EmptySha256, 0,
            "application/pdf", SourceRightsClass.PrivateCopyright);
        Assert.Throws<ArgumentException>(() => new ImportDocumentCommand(context, "doc-fixture", "source-fixture",
            DocumentKind.Book, "synthetic", foreign, metadata));
        Assert.Null(metadata.GeoContext);
        Assert.Null(metadata.PublicationDateText);
    }
}
