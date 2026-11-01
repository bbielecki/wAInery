# G1-03c Knowledge contract-change proposal

IO accepted for freeze issue27. Compiled Contracts become authoritative; this
document preserves the reviewed proposal and future owner test scenarios.

Issue #23, Gate 1 #10; Stream C / Knowledge owner; BR-01/02/09/10.
Status: PROPOSED to Architect / Integration Owner, not frozen or implemented.
Branch: `agent/knowledge/g1-03c-contract-proposal`; API/model cost: 0.
Only this handoff is changed. No OCR, retrieval, persistence, shared C# or schema
implementation is authorized here. Independent QA and IO approval remain required.

## Canonical sources and ownership

The [Knowledge component](../../docs/30-components/Knowledge.md),
[retrieval LLD](../../docs/90-lld/knowledge-and-retrieval.md),
[Polish evidence policy](../../docs/90-lld/polish-context-evidence-policy.md),
[ports/freeze rules](../../docs/90-lld/ports-and-compatibility.md) and approved
[HLD](../../docs/10-architecture/business-hld.md) control semantics. This proposal
specifies a missing IPC seam; it does not replace those documents or add policy.

Knowledge owns documents, versions, extraction/index publication, document
restrictions and citation resolution. Connectivity alone owns source registry,
source-level approval/provenance and source-policy version. Operational facts
come from Vineyard/Winery/Observations/Weather ports, never from this index.
Decisions owns applicability/ranking; AIExecution independently enforces current
rights before any external call. Knowledge.Contracts references BuildingBlocks
only; source resolution is performed by its implementation through the owner port.

## Shared IO proposals, agreed in scope with Stream B

Reuse current `WorkspaceId`, `SubjectRef`, `EvidenceRef`, `SourceVersionRef`,
`CommandContext`, `Quantity`, `Result<T>` and (after PR #21) `CommandReceipt`.
The following names are proposals for IO, not a second shared contract definition:

```csharp
// BuildingBlocks, IO-owned. Map persisted labels explicitly, never by enum integer.
public enum SourceRightsClass
{
    Unknown, PrivateCopyright, PrivateDerived, PrivateOperational,
    PublicLicensed, PublicReference
}
public enum ExternalAiPolicy { Deny, Review, Allow }
public sealed record SourceRightsSnapshot(
    WorkspaceId Workspace, string SourceId, long RegistryVersion,
    SourceRightsClass RightsClass, ExternalAiPolicy ExternalAiPolicy,
    string? AuthorizationRef, DateTimeOffset CheckedAt);

public sealed record StoredObjectRef(
    WorkspaceId Workspace, string Key, string Sha256, long LengthBytes,
    string MediaType, SourceRightsClass Classification);
public sealed record ObjectWriteRequest(
    CommandContext Context, string MediaType, SourceRightsClass Classification,
    long MaxBytes, string? ExpectedSha256);
public sealed record ObjectRead(StoredObjectRef Object, Stream Content);
public interface IObjectStorage
{
    Task<Result<StoredObjectRef>> PutAsync(ObjectWriteRequest request, Stream content, CancellationToken ct);
    Task<Result<StoredObjectRef>> GetInfoAsync(WorkspaceId workspace, string key, CancellationToken ct);
    Task<Result<ObjectRead>> OpenReadAsync(WorkspaceId workspace, string key, long maxBytes, CancellationToken ct);
}
```

The five persisted rights labels are exactly `PRIVATE_COPYRIGHT`,
`PRIVATE_DERIVED`, `PRIVATE_OPERATIONAL`, `PUBLIC_LICENSED`, `PUBLIC_REFERENCE`;
SQL policies are `DENY`, `REVIEW`, `ALLOW`. `Unknown` is fail-closed in-process
metadata, not a new SQL value or a new DomainChange JSON v1 enum member.
`RegistryVersion` is the source configuration/rights revision; it is distinct
from a document/content `SourceVersionRef.Version` and index version.
Default policy is Deny. Missing/unknown class, unknown enum or Review cannot grant
egress. Allow requires a nonblank verified source authorization reference; a
caller-provided snapshot is never proof of permission. Knowledge may tighten a
document/derivative restriction, never widen source approval. Derived chunks and
embeddings keep their parent source refs and restrictions.

Object rules: workspace-scoped opaque keys, lowercase 64-hex SHA256, length >= 0,
nonblank media type and positive caller byte limit bounded again by host policy.
No absolute path, filename guessing or caller filesystem path is exported.
Storage implementation must verify hash/actual length, enforce limits for streams
without Length/Seek, use atomic local write, and contain normalized/symlink-resolved
paths under its private root. This is a future implementation requirement.
Put never disposes the caller's input stream; caller disposes successful OpenRead
Content (including cancellation/error after opening). Missing key returns NOT_FOUND;
zero-byte content is a successful object, which the document parser can reject
separately. Too-large content fails VALIDATION without publishing partial object.
Same bytes in another workspace never authorize a cross-workspace read.
IObjectStorage is not an LLM tool or rights authority: its callers enforce access
and purpose before opening protected bytes. Descriptor classification never grants
permission and dedupe cannot downgrade an existing restriction.

## Knowledge-owned proposed IPC v1 surface

All collection inputs are snapshotted and all output arrays immutable; identity,
scope and confirmation/rights metadata use get-only properties in final code.
Below are declaration sketches, not compiling application code or direct JSON
serialization models. Final names/limits are frozen by IO with consumer tests.

```csharp
// namespace wAInery.Modules.Knowledge.Contracts;
// using wAInery.BuildingBlocks; using System.Collections.Immutable;
public enum EvidencePurpose { LocalOwnerAnalysis, ExternalModelContext, CitationDisplay }
public enum DocumentKind { Book, CuratedWeb, Study, Label }
public enum PublicationState { Unknown, Current, Superseded, Retracted }
public enum TagCoverage { Unknown, Partial, Reviewed }
public sealed record EvidenceContext(
    SubjectRef Subject, ImmutableArray<string> TopicTags,
    ImmutableArray<string> CultivarTags, ImmutableArray<string> PhenologyTags,
    ImmutableArray<string> WineProcessTags, string? CountryCode, string? Region);
public sealed record KnowledgeQuery(
    WorkspaceId Workspace, EvidenceContext Context, string QueryText,
    EvidencePurpose Purpose, DateTimeOffset AsOf, int TopK, int TokenBudget,
    string? TargetModelProfileId, string? Cursor);
public sealed record CitationAnchor(
    string DocumentId, string DocumentVersion, string ChunkId, string Locator,
    int? PdfPageStart, int? PdfPageEnd, string? PrintedPageStart,
    string? PrintedPageEnd, string? SectionPath);
public sealed record SourceGeoContext(string? CountryCode, string? Region, decimal? AltitudeMetres);
public sealed record ClimateMetricDto(string Code, Quantity Value, string? Period, EvidenceRef Provenance);
public sealed record EvidenceMetadata(
    string Title, string? Publisher, string? Author, string? PublicationDateText,
    DateTimeOffset CapturedAt, string? Language, Uri? SourceUrl,
    ImmutableArray<string> TopicTags, ImmutableArray<string> CultivarTags,
    ImmutableArray<string> PhenologyTags, ImmutableArray<string> WineProcessTags,
    SourceGeoContext? GeoContext, ImmutableArray<ClimateMetricDto> ClimateEvidence,
    string? MethodQuality, string? CurrencyRisk,
    ImmutableArray<string> TransferabilityNotes, PublicationState PublicationState,
    TagCoverage TagCoverage, string ContentSha256);
public sealed record DocumentRightsDto(
    SourceRightsSnapshot Source, SourceRightsClass ContentClass, ExternalAiPolicy DocumentRestriction,
    long DocumentPolicyVersion, string? AuthorizationRef, DateTimeOffset CheckedAt);
public sealed record EvidenceItem(
    WorkspaceId Workspace, EvidenceRef Reference, SourceVersionRef SourceVersion,
    CitationAnchor Anchor, string IndexVersion, EvidenceMetadata Metadata,
    DocumentRightsDto Rights, DateTimeOffset KnownAt, string? PermittedExcerpt,
    ImmutableArray<string> RelevanceNotes, ImmutableArray<EvidenceRef> RelatedEvidence);
public sealed record EvidencePage(
    WorkspaceId Workspace, DateTimeOffset AsOf, DateTimeOffset? KnownAt,
    ImmutableArray<EvidenceItem> Items, string? NextCursor,
    ImmutableArray<string> CoverageGaps, int? TokenCount, string? TokenizerVersion);
public sealed record ResolveCitationQuery(
    WorkspaceId Workspace, EvidenceRef Reference, EvidencePurpose Purpose,
    DateTimeOffset AsOf, string? TargetModelProfileId);
public sealed record ResolvedCitation(
    WorkspaceId Workspace, EvidenceRef Reference, SourceVersionRef SourceVersion,
    CitationAnchor Anchor, StoredObjectRef Original, EvidenceMetadata Metadata,
    DocumentRightsDto Rights, DateTimeOffset KnownAt);
public interface IKnowledgeRetriever
{
    Task<Result<EvidencePage>> RetrieveAsync(KnowledgeQuery query, CancellationToken ct);
    Task<Result<ResolvedCitation>> ResolveCitationAsync(ResolveCitationQuery query, CancellationToken ct);
}

public sealed record ImportDocumentCommand(
    CommandContext Context, string DocumentId, string SourceId, DocumentKind Kind,
    string Title, StoredObjectRef Original, EvidenceMetadata Metadata);
public sealed record DocumentImportReceipt(
    string DocumentId, string CandidateVersionId, long AggregateVersion,
    DateTimeOffset RecordedAt, bool Duplicate);
public sealed record AcceptDocumentVersionCommand(
    CommandContext Context, string DocumentId, string CandidateVersionId,
    SourceVersionRef SourceVersion, string ParserVersion);
public sealed record RequestExtractionCommand(
    CommandContext Context, string DocumentId, string DocumentVersion, string ParserVersion);
public sealed record DocumentVersionRef(string DocumentId, string Version);
public sealed record PublishIndexCommand(
    CommandContext Context, string IndexVersion,
    ImmutableArray<DocumentVersionRef> DocumentVersions, string? EmbeddingSpaceVersion);
public sealed record UpdateDocumentRightsCommand(
    CommandContext Context, string DocumentId,
    ExternalAiPolicy Restriction, string? VerifiedAuthorizationRef);
public interface IKnowledgeCommandPort
{
    Task<Result<DocumentImportReceipt>> ImportDocumentAsync(ImportDocumentCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> AcceptVersionAsync(AcceptDocumentVersionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> RequestExtractionAsync(RequestExtractionCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> PublishIndexAsync(PublishIndexCommand command, CancellationToken ct);
    Task<Result<CommandReceipt>> UpdateRightsAsync(UpdateDocumentRightsCommand command, CancellationToken ct);
}
```

Import registers a candidate from an already captured local object, never a
performed operation. It starts with document restriction Deny. Source existence,
workspace and rights are looked up through Connectivity; callers cannot assert
registry UUID or approval. AcceptVersion validates retained object's checksum,
source lineage and parser/version; candidate/accepted IDs remain opaque. Extraction
receipt means a durable local job was accepted, not OCR completed. PublishIndex
atomically switches a fully validated immutable manifest; failure retains previous
published index. A lexical-only index has null EmbeddingSpaceVersion, not a fake
vector version. Manifest lists document ID + version pairs unambiguously through
DocumentVersionRef; two documents may both have version `v1` without collision.

UpdateRights is an explicit authorized owner action with expected version and
source-level approval verification for any Allow. It cannot grant source-level
rights or override Deny/Review. Restriction change and safe outbox metadata are
atomic; restrictive changes invalidate dependent evidence immediately. Index
publication carries affected refs/tags and unknown tag coverage for a bounded
coverage audit. No passage, OCR, embeddings, full prompts or private narrative
belongs in these events, logs or ordinary CI artifacts.

## Boundary and temporal invariants

- Workspace must be nonempty; Context.Subject.Workspace must equal outer workspace;
  source, document, index, object, citation and cursor must resolve in that scope.
  Public IDs stay opaque strings and cannot be reused for another object.
- Query AsOf is knowledge availability time, not publication date or a claim that
  an old permission remains valid. Evidence requires source accepted/captured and
  index published by AsOf. KnownAt reports the actual maximum included availability
  watermark (null when unavailable/empty with no published watermark). No future
  index or later accepted correction enters an earlier view.
- Citation Reference.SourceId/Version must agree with SourceVersionRef and its
  retained document/content lineage. Registry policy revision remains separate.
  PDF page coordinates are 1-based original PDF pages; printed labels are separate.
  Web citation uses retained captured version/section/URL without invented pages.
- Current rights are rechecked at retrieval, resolution and immediately before
  egress by AIExecution. An old ALLOW/as-of snapshot cannot bypass later revocation.
  Effective egress requires current source ALLOW, current document ALLOW and a
  verified approval valid for purpose/target profile. Deny wins; Review is blocked.
  Rights snapshots provide audit provenance, never a reusable authorization token.
- ExternalModelContext requires a target profile for service-terms/purpose checks.
  External retrieval filters denied evidence items entirely, with safe coverage-gap
  code SOURCE_POLICY_DENIED and no private source names/IDs in that gap. Empty
  filtered success remains distinct from no match. Explicit resolution of a denied
  reference returns POLICY_DENIED without its excerpt/metadata/object key. A denied
  excerpt is never replaced by empty text suggesting accessible evidence.
  Local owner retrieval can cite local-only evidence subject to current local access.
  Metadata retains its source classification; metadata-only results are not an
  implicit grant to send private source identifiers/titles to an external model.
- TopK and TokenBudget must be positive and bounded by configured IO-owned limits;
  no numeric default is imposed here. Cursor is bound to workspace/query/asOf/index
  versions. Source filters are copied, unknown metadata stays null/Unknown, no zero
  for missing climate values and no universal numerical source-quality probability.
  Unknown token count must not count as zero or bypass the cap; fail safely or use a
  declared conservative bounded representation. Return tokenizer version when used.
- Empty successful retrieval differs from UNAVAILABLE. CoverageGaps explains missing
  evidence/locale/method; absence of matches is not evidence that an action is safe.
  PublicationState/currency/transferability are retained even for conflicting sources;
  Decisions applies action constraints, never an opaque hardcoded country preference.
- VALIDATION for malformed input/scope, NOT_FOUND for unknown scoped identity/key,
  CONFLICT for stale expected version/changed replay, POLICY_DENIED for rights,
  UNAVAILABLE/RETRYABLE_FAILURE for temporary extraction/storage failures, with safe
  correlation ID and retry flag. No raw provider/SQL/path/content in error messages.

## Compatibility, persistence and consumer cutover

This adds IPC declarations to currently empty Knowledge.Contracts; five existing
JSON v1 schemas/fixtures remain unchanged. EvidenceRef and SourceVersionRef wire
mapping stays under IO; do not serialize these IPC records as existing HTTP DTOs.
No new HTTP endpoint, schema enum, probability or legal-use authorization is implied.

The current published migration 001 has documents/versions/chunks/index metadata,
but it is not proof of complete alias mapping, accepted/known-at history, current
policy versioning, index manifests or citation/file ownership. IO must sequence any
necessary additive migration and actual upgrade/replay tests in a later approved
implementation task; never edit published 001/002 or have module owners write foreign
tables. Knowledge's accepted writes and processing.outbox use one DB transaction.
File write precedes DB reference acceptance; a failed DB write may leave a private
unreferenced blob, never an accepted dangling reference. Cleanup needs its own safe,
reviewed policy; this proposal introduces no destructive cleanup operation.

Consumers: Decisions uses retrieval and citations; AIExecution receives only policy-
tagged approved context through its Decisions-owned reasoning seam; Connectivity
submits local object/source references. None imports Knowledge implementation.
Module implementers rebase after IO freeze merge, acknowledge exact types/semantics,
and run owner tests before combined PostgreSQL/contract/vertical integration tests.
This proposal is not permission to begin Gate 4 before human milestone approval.

## Synthetic fixture and executable-test design

Use generated `book-fixture-A`, `web-fixture-A`, source `source-fixture-A`, opaque
`doc-fixture-A`/`v1`, marker `SYNTHETIC_PRIVATE_MARKER_K23` and workspace A/B. Generate
PDF/text/bytes at test time; retain no copyrighted or real operational material.

| Fixture ID | Producer/consumer input | Required assertion and intended level |
|---|---|---|
| K23-C01 | Mutable tags passed to KnowledgeQuery | DTO snapshots filters; later caller mutation cannot change request; unit/contract |
| K23-C02 | A query with B subject/object/source/cursor | Scope rejected or unknown scoped resource NOT_FOUND; no B data returned; contract + actual owner integration |
| K23-C03 | Index published at 11:00, query AsOf10:59 | Later index excluded; older available publication selected, real PostgreSQL integration |
| K23-C04 | Source metadata lacks climate/publication/method | Null/Unknown preserved, no fabricated date/zero/local validation; contract |
| K23-C05 | PDF page2 label7 and web section anchor | Resolve exact retained doc/version/hash and distinct printed/PDF labels; owner integration |
| K23-C06 | LocalOnly synthetic marker with external purpose | No marker in request/log/event, no provider call; LocalOwnerAnalysis may cite retained local original; component + AI integration |
| K23-C07 | Historical ALLOW followed by current DENY | Old AsOf cannot revive external permission; fresh rights check blocks egress and invalidates dependent evidence; integration |
| K23-C08 | Unknown class, invalid enum or REVIEW | Fail closed before external request; zero model invocations; contract/component |
| K23-C09 | Source DENY, document requests ALLOW | Cannot widen source grant; rights update denied, old policy unchanged; owner integration |
| K23-C10 | Empty source match versus store offline | Success empty with gap differs from UNAVAILABLE; no fabricated evidence or safe-action claim; contract/component |
| K23-C11 | Non-seekable oversized stream or bad expected hash | Put bounded, partial object not published; input stream remains caller-owned; storage integration |
| K23-C12 | Missing key versus zero-byte object | NOT_FOUND versus success length0; successful output stream disposed by caller; storage integration |
| K23-C13 | Same import/idempotency key; then changed payload | First replay same receipt/no extra canonical/outbox effect; changed replay CONFLICT; actual PostgreSQL integration |
| K23-C14 | Invalid new index or local OCR failure | Previous active index still readable; no partly published manifest; actual PostgreSQL integration |
| K23-C15 | Conflicting PL/foreign sources, injection text | Keep provenance/locality/limitations, treat injected instructions as data; no tool/write side effect; component + expert-reviewed evaluation later |
| K23-C16 | Same content in A and B, key traversal/symlink | No cross-workspace read or root escape; safe error contains neither path nor marker; storage integration |
| K23-C17 | Missing token count or requested excessive bounds | Unknown is not zero; bounded result or safe failure, no unbounded context; contract/component |

These tests are DESIGNED, NOT EXECUTED here. No fake retriever/storage implementation
is presented as proof of source resolution, egress, as-of behavior or citation quality.
IO should first compile the DTO tests and prove RED, then implement guards and run
producer/consumer tests; later owner implementations must pass the named real tests.

## Actual verification, risks and learning

Actual source-consistency check: `python -` with standard-library assertions read
this file and published 001, matched all five rights labels/three policies,
resolved every relative Markdown link, checked balanced code fences, verified all
10 proposed async methods carry CancellationToken, paired manifest identities,
nullable empty watermark and explicit content classification. Result: PASS.
`git diff --no-index --check -- /dev/null <this-file>` produced no whitespace error;
its exit1 denotes the expected new-file difference. Final `git diff --cached --check`
passed; `git status --short` listed only this one allowed file. No dotnet, PostgreSQL, OCR, retrieval,
LLM, evaluation or application demo was run by this proposal author. Parent's prior
59 tests are not this task's execution evidence. No paid/live service or private data.

Open IO decisions: final shared names/limits and source snapshot adaptation.
DocumentVersionRef/index manifest uses explicit paired opaque identity.
Policy provenance persistence and race-safe pre-egress revalidation need future
implementation proof. Source-level and document-level ownership remains unchanged.
Independent QA has not yet reviewed this proposal. No PR/push/merge by this session.
No transferable finding: this is contract analysis rather than verified implementation
experience. Existing contract-change skill reused; no new skill or savings claimed.
