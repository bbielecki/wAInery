# G1-03c Connectivity contract-change proposal

Issue [#22](https://github.com/bbielecki/wAInery/issues/22), milestone #10; BR-08/09/10
(and source extensibility BR-06). Owner: Stream B / Connectivity; author session
`/root/stream_b_connectivity`; Architect / Integration Owner decides the freeze.
Status: IO accepted for freeze issue27. Compiled Contracts become authoritative;
this document preserves the reviewed proposal and future owner test scenarios.
Application API cost: 0. Scope is this handoff only; no source, schema, fixtures,
shared knowledge or provider settings changed. Base: `281ec1d`.

## Purpose and compatibility

A known workspace-scoped source can supply a durable raw receipt, be parsed with
pinned versions, and produce owner-bound drafts. Only the canonical owner accepts
facts or confirms performed operations. Connectivity never writes owner tables,
marks a narrative as performed, or sends private bytes externally by default.

Current `Connectivity.Contracts` contains no compiled DTOs/ports. The proposed
surface is new IPC v1, not a new HTTP/JSON wire contract. Existing five JSON v1
schemas and opaque ID fixtures remain unchanged. All public source/receipt/run/
draft IDs and object keys are stable opaque strings; caller IDs are never parsed
as UUIDs or resolved by display name. Canonical internal IDs come only from the
owner resolution result. Shared types from PR #21 (`SubjectCommand`,
`ResolvedSubject`, `CommandReceipt`, `OperationConfirmation`) are reused after
merge; do not fork their definitions in this module.

Canonical references: [Connectivity](../../docs/30-components/Connectivity.md),
[source adapters](../../docs/90-lld/source-adapters.md),
[ports](../../docs/90-lld/ports-and-compatibility.md),
[freeze/registry ownership](../../docs/90-lld/implementation-contract-freeze.md),
[contract catalog](../../docs/40-contracts/catalog.md). Those win over this proposal.

## IO-owned shared value requests

Coordinate these names with Knowledge's proposal #23. Connectivity owns source
approval/history; Knowledge may tighten document/derivative restrictions. No
Contracts assembly imports another module's Contracts. Implementations may do so.

- `SourceRightsClass`: `Unknown`, `PrivateCopyright`, `PrivateDerived`,
  `PrivateOperational`, `PublicLicensed`, `PublicReference`. Explicit SQL mapping
  preserves exactly the five existing `registry.sources.rights_class` values;
  `Unknown` is an in-process fail-closed sentinel, never silently mapped to public.
- `ExternalAiPolicy`: `Deny`, `Review`, `Allow`, mapped explicitly to `DENY`,
  `REVIEW`, `ALLOW`; `Review` does not authorize a call. Default is `Deny`.
- Immutable `SourceRightsSnapshot(Classification, ExternalAiPolicy, PolicyVersion,
  KnownAt, BasisRef?, AuthorizedBy?, AuthorizedAt?)`: source policy proof is
  versioned metadata, not an instruction from an extracted document. `ALLOW`
  requires verified explicit approval/basis; DTO construction alone grants nothing.
- Immutable `StoredObjectRef(Workspace, Key, Sha256, ByteLength)`: validated opaque
  key, 64 lowercase hex SHA256, nonnegative length; no local absolute path or raw
  bytes in an event. The shared `IObjectStorage` belongs to IO. It validates
  workspace, key/root traversal, hash/length and access on actual use. The same
  shape can serve raw input and privately stored typed draft payloads.

A historical rights snapshot supports audit only. New external egress must also
check current source/document policy; replay of an old ALLOW cannot bypass a later
revocation. Unknown policy/version or failed current-policy lookup denies egress.
This preserves accepted policy; it proposes no new grant or broader permission.

Explicit class persistence mapping:

| IPC enum | Existing SQL value |
|---|---|
| PrivateCopyright | PRIVATE_COPYRIGHT |
| PrivateDerived | PRIVATE_DERIVED |
| PrivateOperational | PRIVATE_OPERATIONAL |
| PublicLicensed | PUBLIC_LICENSED |
| PublicReference | PUBLIC_REFERENCE |
| Unknown | No stored mapping; deny/quarantine, never public fallback |

## Minimal requested C# surface

The following is a complete signature/field proposal, not code to copy as an
implementation. Collections must be immutable snapshots. IO implements required
scope/identity/enum/version/length guards; handler validation still checks actual
existence, actor authority, installed versions and payload schema. All commands
require non-null `CommandContext`; writes requiring existing state enforce its
`ExpectedVersion`. All times are UTC; limits come from configured technical caps.

```csharp
// Assumed IO values: CommandContext, WorkspaceId, SubjectRef, Result<T>,
// ResolvedSubject, CommandReceipt, SourceRightsSnapshot, StoredObjectRef.
using System.Collections.Immutable;
namespace wAInery.Modules.Connectivity.Contracts;

public enum SourceStatus { Active, Degraded, Disabled }
public enum ReceiptStatus { Received, Parsed, Proposed, Accepted, Rejected, Failed }
public enum ImportRunStatus { Pending, Running, Succeeded, Failed }
public enum DraftTarget
{
    Observation, VineyardActivityProposal, CellarOperationProposal,
    ForecastRun, DocumentVersion
}
public sealed record SourceCapabilitiesDto(
    bool Manual, bool Poll, ImmutableArray<DraftTarget> Targets,
    ImmutableArray<string> ExpectedMeasurementTypes, string PayloadSchemaVersion,
    bool UsesSecretReference);
public sealed record SourceSnapshotDto(
    WorkspaceId Workspace, string SourceId, long Version, string SourceKind,
    string DisplayName, SourceStatus Status, string AdapterKind,
    string AdapterVersion, string MappingVersion, string ConfigurationRef,
    SourceCapabilitiesDto Capabilities, SourceRightsSnapshot Rights,
    DateTimeOffset AsOf, DateTimeOffset KnownAt);
public sealed record ResolvedSourceDto(
    ResolvedSubject Identity, SourceSnapshotDto Source);

// Registration always creates policy DENY; widening requires the next command.
public sealed record RegisterSourceCommand(
    CommandContext Context, string SourceId, string SourceKind, string DisplayName,
    string AdapterKind, string AdapterVersion, string MappingVersion,
    string ConfigurationRef, SourceRightsClass RightsClass, string? RightsBasisRef);
public sealed record UpdateSourceRightsCommand(
    CommandContext Context, string SourceId, SourceRightsClass RightsClass,
    ExternalAiPolicy RequestedPolicy, string BasisRef);
public sealed record SetSourceStatusCommand(
    CommandContext Context, string SourceId, SourceStatus Status, string ReasonCode);

public sealed record CaptureReceiptCommand(
    CommandContext Context, string SourceId, long SourceDefinitionVersion, string ExternalKey,
    StoredObjectRef Payload, string AdapterVersion, string MappingVersion,
    DateTimeOffset? SourcePublishedAt, string? SourceRecordVersion, string? SupersedesReceiptId);
public sealed record SourceReceiptDto(
    WorkspaceId Workspace, string ReceiptId, string SourceId, long SourceDefinitionVersion, string ExternalKey,
    StoredObjectRef Payload, string AdapterVersion, string MappingVersion,
    DateTimeOffset? SourcePublishedAt, string? SourceRecordVersion, DateTimeOffset ReceivedAt,
    DateTimeOffset RecordedAt, ReceiptStatus Status, string? ReasonCode,
    string? SupersedesReceiptId, bool Duplicate);
public sealed record ProcessReceiptCommand(
    CommandContext Context, string ReceiptId, string AdapterVersion,
    string MappingVersion);
public sealed record ReplayImportCommand(
    CommandContext Context, string SourceId, string OriginalRunId, string ReasonCode);
public sealed record ImportRunDto(
    WorkspaceId Workspace, string RunId, string SourceId, long SourceDefinitionVersion,
    ImmutableArray<string> ReceiptIds, string AdapterVersion, string MappingVersion,
    string? OriginalRunId, ImportRunStatus Status, DateTimeOffset RecordedAt,
    DateTimeOffset? CompletedAt, string? ErrorCode);

// Payload is a private object containing a validated owner-contract candidate.
// It is not arbitrary JSON CRUD; schema IDs/targets are an installed allowlist.
public sealed record DraftPayloadRef(
    StoredObjectRef Object, string OwnerContractId, int OwnerContractVersion);
public sealed record ImportDraftDto(
    WorkspaceId Workspace, string DraftId, string RunId, string ReceiptId,
    DraftTarget Target, SubjectRef? ProposedSubject,
    DateTimeOffset? ProposedEffectiveAt, DraftPayloadRef Payload,
    ImmutableArray<string> MissingFields, ImmutableArray<EvidenceRef> EvidenceRefs);
public sealed record DraftPageDto(
    WorkspaceId Workspace, string RunId, DateTimeOffset AsOf,
    DateTimeOffset KnownAt, ImmutableArray<ImportDraftDto> Items, string? NextCursor);

public interface ISourceRegistryReadPort
{
    Task<Result<ResolvedSourceDto>> ResolveAsync(
        WorkspaceId workspace, string sourceId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> GetSourceAsync(
        WorkspaceId workspace, string sourceId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<SourceRightsSnapshot>> GetCurrentRightsAsync(
        WorkspaceId workspace, string sourceId, CancellationToken ct);
}
public interface IConnectivityReadPort
{
    Task<Result<SourceReceiptDto>> GetReceiptAsync(
        WorkspaceId workspace, string receiptId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<ImportRunDto>> GetImportRunAsync(
        WorkspaceId workspace, string runId, DateTimeOffset asOf, CancellationToken ct);
    Task<Result<DraftPageDto>> GetDraftsAsync(
        WorkspaceId workspace, string runId, DateTimeOffset asOf,
        int maxCount, string? cursor, CancellationToken ct);
}
public interface IConnectivityCommandPort
{
    Task<Result<SourceSnapshotDto>> RegisterSourceAsync(RegisterSourceCommand command, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> UpdateSourceRightsAsync(UpdateSourceRightsCommand command, CancellationToken ct);
    Task<Result<SourceSnapshotDto>> SetSourceStatusAsync(SetSourceStatusCommand command, CancellationToken ct);
    Task<Result<SourceReceiptDto>> CaptureReceiptAsync(CaptureReceiptCommand command, CancellationToken ct);
    Task<Result<ImportRunDto>> ProcessReceiptAsync(ProcessReceiptCommand command, CancellationToken ct);
    Task<Result<ImportRunDto>> ReplayImportAsync(ReplayImportCommand command, CancellationToken ct);
}

public sealed record SourceFetchRequest(
    SourceSnapshotDto Source, string? Cursor, int MaxRecords);
public sealed record FetchedRecordDto(
    string ExternalKey, StoredObjectRef Payload, DateTimeOffset RetrievedAt,
    DateTimeOffset? SourcePublishedAt, string? SourceRecordVersion, string? SupersedesReceiptId);
public sealed record SourceFetchPageDto(
    ImmutableArray<FetchedRecordDto> Items, string? ProposedNextCursor,
    DateTimeOffset RetrievedAt, bool NotModified);
public sealed record SourceParseRequest(
    SourceSnapshotDto Source, SourceReceiptDto Receipt, string RunId,
    string AdapterVersion, string MappingVersion);
public sealed record ParsedDraftBatchDto(
    WorkspaceId Workspace, string RunId, ImmutableArray<ImportDraftDto> Items);
public interface ISourceAdapter
{
    string AdapterKind { get; }
    string AdapterVersion { get; }
    SourceCapabilitiesDto Capabilities { get; }
    Task<Result<SourceFetchPageDto>> FetchAsync(SourceFetchRequest request, CancellationToken ct);
    Task<Result<ParsedDraftBatchDto>> ParseAsync(SourceParseRequest request, CancellationToken ct);
}
```

`SourceCapabilitiesDto.Targets` comes from trusted installed adapter configuration,
not model output. Parsing may propose only an allowed target; operation targets
are named `Proposal` and there is no confirmed-operation target or mutable
`IsConfirmed` flag. Draft payload schema names for owner commands without a wire
schema remain IO freeze prerequisites; do not invent existing schema URLs. Actual
normalizers/dispatchers in Connectivity's implementation may import owner
Contracts, validate/map the candidate and call owner ports. Vineyard/Winery own
PrepareConfirmation/Confirm and token validation. Their acceptance result is the
truth; successful parsing or a SourceReceipt marked PROPOSED is not performed work.

Fetch may store private bytes through IO's local object storage and returns only
references. It creates no receipt or canonical owner fact by itself. Receipt
capture verifies the returned object's workspace/hash/length before accepting
metadata. A failed capture may leave an unreferenced local object; cleanup needs a
separate scoped retention procedure, not a destructive rollback of owner state.
Caller never receives private filesystem roots, raw credentials or auth headers.
A private ConfigurationRef resolves through local protected config; it is not a
serialized settings dictionary or arbitrary adapter type/HTTP execution request.

## Identity, time, idempotency and errors

- Resolve uses `(workspace, "Connectivity", "Source", publicSourceId)` and returns
  `ResolvedSubject` with matching scope, internal UUID and positive source version.
  Unknown/wrong-workspace source returns `NOT_FOUND`; never guess by name.
- A raw receipt is immutable at `(workspace, source, externalKey)`. Same key and
  same content hash returns the original ID with Duplicate=true and no new owner
  write/event. Same key/different content returns `CONFLICT`; a source revision
  uses a new explicit external key and scoped SupersedesReceiptId.
- Parse identity adds pinned source-definition/adapter/mapping versions to the receipt. Replay appends
  a run referencing OriginalRunId and the original versions; changing a mapping
  is explicit ProcessReceipt, not a silent replay with today's parser. Stable
  owner idempotency keys prevent repeat acceptance for the same receipt/version;
  corrections create new owner facts. Replay never bypasses operation confirmation.
- ReceivedAt/RecordedAt are authoritative local server acceptance times, not
  caller-backdated values. SourcePublishedAt/ProposedEffectiveAt remain distinct.
  `asOf` queries include only versions known by that time. No future source
  publication/forecast/observation is made known by changing its event time.
- Fetch failure/offline/429 does not commit ProposedNextCursor. Advance the saved
  cursor only after durable capture of the entire accepted page. Bounded retries
  and failure jobs use shared processing infrastructure; no new broker or loop.
- Errors use existing `VALIDATION`, `NOT_FOUND`, `CONFLICT`, `POLICY_DENIED`,
  `UNAVAILABLE`, `RATE_LIMITED`, `BUDGET_EXCEEDED`, `RETRYABLE_FAILURE` and correlation
  IDs, with safe codes/messages only. Unknown source/versions/units/targets are
  rejected or quarantined before canonical acceptance. Source-level rejected
  receipts use existing REJECTED + reason code; no invented stored status.

## Persistence proposals and IO decisions required

No migration is part of this task. Actual immutable 001 has no stable public
source-ID mapping, source version-history table, import-run/checkpoint table or
receipt recorded-at/supersession columns. Integration Owner must choose reviewed
additive migrations before these handlers are implemented. Never modify 001.

Proposed identity storage: unique `(workspace_id,public_source_id)` on the
Connectivity-owned source mapping, retained when disabled so IDs cannot be reused;
map to `registry.sources.id` rather than UUID-parsing a public string. Whether this
is a column or owner key table is IO's physical-design decision. Legacy rows need
explicit mapping/backfill, never display-name inference.

Existing receipt uniqueness is `(workspace_id,source_id,external_key)` and status
values are exactly those above. Keep that raw boundary; model normalization/run
identity separately by receipt + pinned versions. Receipt/run source-definition versions and optional provider record version must be persisted separately from adapter/mapping versions. SourceSnapshot rights/history
and registry.version updates must be atomic with durable metadata-only outbox
changes. Knowledge's document-rights events must reflect inherited source-policy
restrictions; IO coordinates source-policy notification without a new policy owner.
No direct cross-owner table queries/writes or new universal canonical table.

## Synthetic verification plan and cutover

All cases below are planned; they have not run against absent handlers. IO freezes
DTO producer/consumer fixtures first, then each owner tests its implementation,
then Integration Owner/independent QA runs cross-module tests before merge.

| ID | Synthetic setup | Required positive/negative assertion |
|---|---|---|
| CON-SRC-01 | workspace A, `manual-reading`, internal synthetic UUID | Resolve preserves the opaque ID/version/rights; no Guid.Parse on public ID. Unknown ID and same ID in B return NOT_FOUND. |
| CON-SRC-02 | register each of the five rights classes | Registration defaults DENY; REVIEW/Unknown/unverified ALLOW grant never authorizes external egress. |
| CON-SRC-03 | explicit grant with actor/basis at T2, revoked at T3 | As-of T1 excludes future grant; a new call after T3 cannot reuse historical ALLOW. Concurrent stale ExpectedVersion returns CONFLICT. |
| CON-RAW-01 | generated UTF-8 bytes `synthetic note A`, calculated SHA256 | Same capture key/hash twice yields one receipt/one owner effect; changed hash returns CONFLICT; original bytes/ref retained. |
| CON-RAW-02 | object in workspace B, invalid hash, `../` key or missing object | Capture rejects before source/owner writes; no private path/content in logs/error. |
| CON-PARSE-01 | allowed manual source produces a cellar proposal descriptor | Target remains CellarOperationProposal; missing batch/time/amount stays explicit; no performed operation until owner review/token/confirm. |
| CON-PARSE-02 | malicious payload asks for SQL, shell, confirmed work or foreign target | Installed target/schema allowlist rejects; no owner-schema SQL or unauthorized tool call. |
| CON-TIME-01 | sourcePublished T0, received T2, decision asOf T1 | Receipt/draft excluded at T1 even with earlier effective time; UTC normalization retains actual instants. |
| CON-REPLAY-01 | old run adapter1/mapping1, current mapping2 | Replay stays pinned1; explicit reprocess2 appends provenance, keeps raw record, and cannot create a second performed operation. |
| CON-CURSOR-01 | two fetched records, second capture fails, then retry | Cursor unchanged until page captured; retry dedupes first receipt. Empty successful page differs from provider unavailable. |
| CON-OWNER-01 | generated numeric observation with incompatible unit/source scope | Owner validation rejects/quarantines; no canonical observation/outbox; valid retry yields atomic owner fact + outbox exactly once. |
| CON-RIGHTS-01 | private synthetic document/derived draft | No raw text in event/log/Git; rights inherited by Knowledge; denied provider counter remains zero. |

Contract-only freeze must run build/reference checks and existing v1 schemas, plus
positive/negative constructor/enum/workspace/paging/payload discriminator tests.
Later integration tests must use real PostgreSQL for source mappings/receipt and
run uniqueness, atomicity, as-of history and replay; fake ports cannot prove them.
No live weather, paid model call, real book or operational note is necessary.

Affected consumers: Observations/Weather/Knowledge source resolution; Knowledge
and AIExecution current rights; local CLI source/receipt status; Vineyard/Winery
proposal dispatch; Decisions metadata provenance/invalidation. They cut over only
after IO contract PR merge and rebase; acknowledgements remain pending assignment.

## Evidence and handoff limits

Actual checks for this proposal are recorded below after execution. No .NET build,
contract suite, database, model, provider or business-flow test is claimed here.
Existing parent results from PR #21 are not results of this separate worktree.
No private data or API cost incurred. No transferable finding: schema gaps above
are verified design dependencies, not yet a validated reusable implementation
lesson. Existing wainery-contract-change skill is sufficient; no new skill proposed.
Gate 1 remains incomplete; no authorization for Gate 2/3/4 implementation.

Reproducible static audit (Python, no external dependencies). The first audit
caught missing literal SQL enum mappings in this proposal; the explicit mapping
table fixed that documentation omission. No business implementation is tested.

<!-- connectivity-audit -->
```python
from pathlib import Path
import re
root = Path.cwd()
p = root / 'agents/handoffs/G1-03c-connectivity-contract-change.md'
proposal = p.read_text(encoding='utf-8')
sql = (root / 'src/Persistence/Migrations/001_foundation.sql').read_text(encoding='utf-8')
source = sql.split('CREATE TABLE registry.sources (', 1)[1].split('CREATE TABLE registry.measurement_types', 1)[0]
receipt = sql.split('CREATE TABLE connectivity.source_receipts (', 1)[1].split('CREATE TABLE connectivity.operation_proposals', 1)[0]
def stored_values(block, column):
    return set(re.findall(r"'([^']+)'", re.search(column + r' IN \(([^)]+)\)', block).group(1)))
rights = {'PRIVATE_COPYRIGHT', 'PRIVATE_DERIVED', 'PRIVATE_OPERATIONAL', 'PUBLIC_LICENSED', 'PUBLIC_REFERENCE'}
assert stored_values(source, 'rights_class') == rights
assert all(value in proposal for value in rights)
assert stored_values(source, 'external_ai_policy') == {'DENY', 'REVIEW', 'ALLOW'}
assert "DEFAULT 'DENY'" in source
for enum_name, block in [('SourceStatus', source), ('ReceiptStatus', receipt)]:
    members = re.search(r'public enum ' + enum_name + r' \{([^}]+)\}', proposal).group(1)
    assert {value.strip().upper() for value in members.split(',')} == stored_values(block, 'status')
assert 'UNIQUE(workspace_id,source_id,external_key)' in receipt
assert 'public_source_id' not in source
assert 'Task<Result<ParsedDraftBatchDto>> ParseAsync' in proposal
assert 'ConfirmedOperation' not in re.search(r'public enum DraftTarget\s*\{([^}]+)\}', proposal).group(1)
links = re.findall(r'\]\((\.\.?/[^)#]+)(?:#[^)]*)?\)', proposal)
assert all((p.parent / link).resolve().is_file() for link in links)
assert sum(line.startswith(chr(96) * 3) for line in proposal.splitlines()) % 2 == 0
print('PASS: SQL enum/default/uniqueness consistency, draft boundary, five local links and code fences')
```

Exact static audit invocation and Git checks, run from this task worktree:

```powershell
@'
from pathlib import Path
p = Path('agents/handoffs/G1-03c-connectivity-contract-change.md')
audit = p.read_text(encoding='utf-8').split('<!-- connectivity-audit -->', 1)[1].split('```python', 1)[1].split('```', 1)[0]
exec(compile(audit, str(p) + ':audit', 'exec'))
'@ | python -
git -c safe.directory=C:/Users/bartl/AppData/Local/Temp/wainery-g1-worktrees/connectivity-contract diff --cached --check
git -c safe.directory=C:/Users/bartl/AppData/Local/Temp/wainery-g1-worktrees/connectivity-contract diff --cached --name-only
```
