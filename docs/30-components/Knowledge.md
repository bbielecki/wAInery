# Knowledge — component boundary proposal

## Components / ownership
- `DocumentVault`: accepted book/web versions, bytes, sha256, access class, origin URI, rights policy; private file storage via port.
- `ExtractionPipeline`: local/offline OCR & structural extraction, page anchors, metadata lineage. Separate one-shot job; resumable and versioned.
- `EvidenceIndex`: chunk, chunker/embedding model/index version and full-text; derived index rebuildable without touching domain state.
- `HybridRetriever`: purpose-aware text + vector retrieval, context filters (language/variety/region/source credibility); citation span with doc version and page.
- `RightsPolicy`: `LOCAL_ONLY|EXTERNAL_ALLOWED_EXPLICIT|COMMERCIAL_REVIEWED` plus purpose/owner provenance, derived evidence inherits restrictions.

## Public contracts
`RegisterDocument`, `AcceptDocumentVersion`, `ExtractDocument`, `PublishIndexVersion`, `SearchEvidence(query,subjectContext,purpose,asOf,topK,tokenBudget)`. Retrieval result returns `EvidenceRef`, page range, rights classification and permitted *redacted/approved* excerpt (if policy permits).

## Events
`KnowledgeIndexPublished`, `DocumentRightsChanged` with affected document/index versions, topic/variety/phase applicability tags and reference IDs; no book text in outbox. Missing tags trigger bounded background coverage audit; rights restriction triggers immediate invalidation of dependent plan evidence.

## Data/storage
Schema `knowledge`: `sources`, `document_versions`, `extractions`, `chunks`, `embedding_versions`, `chunk_vectors`, `rights_policies`, `outbox`; local protected document root. Excerpts are not stored in app logs or source control.

## Failure, tests, evolution
OCR job failure quarantines only that document; reference span must resolve to original PDF page in tests; re-embedding creates new index version and atomic active-index switch; initial 1–2 books and curated URLs require no dedicated search engine. Retrieval may return knowledge available only for local analysis; AIExecution must enforce egress independently.
