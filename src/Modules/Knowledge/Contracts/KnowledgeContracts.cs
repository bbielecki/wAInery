using System.Collections.Immutable;
using wAInery.BuildingBlocks;

namespace wAInery.Modules.Knowledge.Contracts;

public enum EvidencePurpose { LocalOwnerAnalysis, ExternalModelContext, CitationDisplay }
public enum DocumentKind { Book, CuratedWeb, Study, Label }
public enum PublicationState { Unknown, Current, Superseded, Retracted }
public enum TagCoverage { Unknown, Partial, Reviewed }
public sealed record EvidenceContext(SubjectRef Subject, ImmutableArray<string> TopicTags,
    ImmutableArray<string> CultivarTags, ImmutableArray<string> PhenologyTags,
    ImmutableArray<string> WineProcessTags, string? CountryCode, string? Region);

public sealed record KnowledgeQuery
{
    public WorkspaceId Workspace { get; }
    public EvidenceContext Context { get; }
    public string QueryText { get; }
    public EvidencePurpose Purpose { get; }
    public DateTimeOffset AsOf { get; }
    public int TopK { get; }
    public int TokenBudget { get; }
    public string? TargetModelProfileId { get; }
    public string? Cursor { get; }
    public KnowledgeQuery(WorkspaceId workspace, EvidenceContext context, string queryText, EvidencePurpose purpose,
        DateTimeOffset asOf, int topK, int tokenBudget, string? targetModelProfileId = null, string? cursor = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText);
        if (workspace.Value == Guid.Empty || context.Subject is null || !context.Subject.HasValidScope || context.Subject.Workspace != workspace)
            throw new ArgumentException("Knowledge subject scope mismatch.");
        if (!Enum.IsDefined(purpose) || topK is < 1 or > 50 || tokenBudget is < 1 or > 16384)
            throw new ArgumentException("Invalid knowledge purpose or bounded evidence request.");
        if (purpose == EvidencePurpose.ExternalModelContext) ArgumentException.ThrowIfNullOrWhiteSpace(targetModelProfileId);
        (Workspace, Context, QueryText, Purpose, AsOf, TopK, TokenBudget, TargetModelProfileId, Cursor) =
            (workspace, context, queryText, purpose, asOf.ToUniversalTime(), topK, tokenBudget, targetModelProfileId, cursor);
    }
}

public sealed record CitationAnchor(string DocumentId, string DocumentVersion, string ChunkId, string Locator,
    int? PdfPageStart, int? PdfPageEnd, string? PrintedPageStart, string? PrintedPageEnd, string? SectionPath);
public sealed record SourceGeoContext(string? CountryCode, string? Region, decimal? AltitudeMetres);
public sealed record ClimateMetricDto(string Code, Quantity Value, string? Period, EvidenceRef Provenance);
public sealed record EvidenceMetadata(string Title, string? Publisher, string? Author, string? PublicationDateText,
    DateTimeOffset CapturedAt, string? Language, Uri? SourceUrl, ImmutableArray<string> TopicTags,
    ImmutableArray<string> CultivarTags, ImmutableArray<string> PhenologyTags, ImmutableArray<string> WineProcessTags,
    SourceGeoContext? GeoContext, ImmutableArray<ClimateMetricDto> ClimateEvidence, string? MethodQuality,
    string? CurrencyRisk, ImmutableArray<string> TransferabilityNotes, PublicationState PublicationState,
    TagCoverage TagCoverage, string ContentSha256);
public sealed record DocumentRightsDto(SourceRightsSnapshot Source, SourceRightsClass ContentClass,
    ExternalAiPolicy DocumentRestriction, long DocumentPolicyVersion, string? AuthorizationRef, DateTimeOffset CheckedAt);
public sealed record EvidenceItem(WorkspaceId Workspace, EvidenceRef Reference, SourceVersionRef SourceVersion,
    CitationAnchor Anchor, string IndexVersion, EvidenceMetadata Metadata, DocumentRightsDto Rights,
    DateTimeOffset KnownAt, string? PermittedExcerpt, ImmutableArray<string> RelevanceNotes, ImmutableArray<EvidenceRef> RelatedEvidence);
public sealed record EvidencePage(WorkspaceId Workspace, DateTimeOffset AsOf, DateTimeOffset? KnownAt,
    ImmutableArray<EvidenceItem> Items, string? NextCursor, ImmutableArray<string> CoverageGaps,
    int? TokenCount, string? TokenizerVersion);
public sealed record ResolveCitationQuery(WorkspaceId Workspace, EvidenceRef Reference, EvidencePurpose Purpose,
    DateTimeOffset AsOf, string? TargetModelProfileId);
public sealed record ResolvedCitation(WorkspaceId Workspace, EvidenceRef Reference, SourceVersionRef SourceVersion,
    CitationAnchor Anchor, StoredObjectRef Original, EvidenceMetadata Metadata, DocumentRightsDto Rights, DateTimeOffset KnownAt);

public interface IKnowledgeRetriever
{
    Task<Result<EvidencePage>> RetrieveAsync(KnowledgeQuery query, CancellationToken ct);
    Task<Result<ResolvedCitation>> ResolveCitationAsync(ResolveCitationQuery query, CancellationToken ct);
}

public sealed record ImportDocumentCommand
{
    public CommandContext Context { get; }
    public string DocumentId { get; }
    public string SourceId { get; }
    public DocumentKind Kind { get; }
    public string Title { get; }
    public StoredObjectRef Original { get; }
    public EvidenceMetadata Metadata { get; }
    public ImportDocumentCommand(CommandContext context, string documentId, string sourceId, DocumentKind kind,
        string title, StoredObjectRef original, EvidenceMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(original);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (original.Workspace != context.Workspace || !Enum.IsDefined(kind)) throw new ArgumentException("Document source/object scope mismatch.");
        (Context, DocumentId, SourceId, Kind, Title, Original, Metadata) = (context, documentId, sourceId, kind, title, original, metadata);
    }
}
public sealed record DocumentImportReceipt(string DocumentId, string CandidateVersionId, long AggregateVersion, DateTimeOffset RecordedAt, bool Duplicate);
public sealed record AcceptDocumentVersionCommand(CommandContext Context, string DocumentId, string CandidateVersionId,
    SourceVersionRef SourceVersion, string ParserVersion);
public sealed record RequestExtractionCommand(CommandContext Context, string DocumentId, string DocumentVersion, string ParserVersion);
public sealed record DocumentVersionRef(string DocumentId, string Version);
public sealed record PublishIndexCommand(CommandContext Context, string IndexVersion,
    ImmutableArray<DocumentVersionRef> DocumentVersions, string? EmbeddingSpaceVersion);
public sealed record UpdateDocumentRightsCommand(CommandContext Context, string DocumentId,
    ExternalAiPolicy Restriction, string? VerifiedAuthorizationRef);
public interface IKnowledgeCommandPort
{
    Task<Result<DocumentImportReceipt>> ImportDocumentAsync(ImportDocumentCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> AcceptVersionAsync(AcceptDocumentVersionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RequestExtractionAsync(RequestExtractionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> PublishIndexAsync(PublishIndexCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> UpdateRightsAsync(UpdateDocumentRightsCommand command, CancellationToken ct);
}
