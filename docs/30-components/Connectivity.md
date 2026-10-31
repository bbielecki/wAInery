# Connectivity — component boundary proposal

## Components / ownership
- `SourceRegistry`: typed source definitions, adapter name, mapping/version, polling schedule, license/restrictions.
- `RawInbox`: append-only intake receipt (checksum, payload reference, source, received timestamp), dedupe and quarantine.
- `SourceAdapters`: `BookFileAdapter`, `CuratedWebAdapter`, `ManualNoteAdapter`, `FreeWeatherAdapter`; later MQTT and laboratory import through same port.
- `ImportDispatcher`: transforms raw envelopes to typed **draft commands** for owning modules; never writes owner schema.
- `SyncScheduler`: local scheduled polling and checkpoint with bounded backoff; resumes after app restart.

## Public contracts
`ISourceAdapter.FetchAsync`/`ParseAsync`, `SubmitRawEnvelope`, `ReplayImport(sourceRunId)`, `SourceRecordAccepted/Rejected`. Dispatch target known from source kind (not open-ended AI-chosen SQL). Draft notes need confirmation before canonical performed operation.

## Events
`RawSourceAccepted` internal, `ImportFailed`, `SourceSyncCompleted` for operations; business events originate only in canonical owner modules after acceptance.

## Data/storage
Schema `connectivity`: `sources`, `raw_receipts`, `import_runs`, `sync_checkpoints`, `quarantine`, `outbox`; payload bytes in local file abstraction, path validated against root, content-addressed objects and checksum.

## Failure, tests, evolution
Retries cannot duplicate operations: stable key `(workspace,sourceId,externalRecordId|rawHash, mappingVersion)`. Network/offline fetch must not destroy checkpoint; replay manual import does not bypass approval. No ingestion middleware cloud cost; adapters are ordinary classes.
