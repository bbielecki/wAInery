# Knowledge & Evidence bounded context

**Class:** Supporting domain. **Status:** Gate 2 accepted. **Provisional owner directory:** `src/Modules/Knowledge/`.

## Purpose
Own accepted original document bytes and manage copyrighted personal books and curated internet evidence with provenance, rights, locale/variety suitability, versioned OCR and retrieval. Knowledge is evidence, not canonical current vineyard state.

## Owns
`KnowledgeSource`, `Document`, `DocumentVersion`, `ExtractionVersion`, `EvidenceSpan`, `Chunk`, `EmbeddingIndexVersion`, `UsageRightsPolicy`, source quality/applicability metadata, retrieval references.

## Business invariants
- Save original document, hash, OCR engine/version and correspondence between extracted spans and PDF page ranges.
- Chunks are derived/rebuildable; preserve raw documents and text versions; no author text or extracted chunks in Git.
- Private books tagged `PRIVATE_COPYRIGHT`; derivatives default to restricted unless manually reviewed; public web availability ≠ commercial redistribution permission.
- Retrieval enforces allowed purpose and evidence scope before returning chunks to an external AI path; data egress is enforced again in AIExecution.
- Scientific credibility and agroclimatic suitability are independent scores/tags; Polish fit cannot be deduced solely from language of source.
- Document changes do not automatically trigger full retraining or world-wide plan recomputation. Track provenance of accepted plan evidence and selectively re-evaluate.

## Consumes
Document/web import envelopes; manually curated relevance tags; user-directed additional URL fetch.

## Publishes (illustrative)
`KnowledgeDocumentIndexed`, `KnowledgeIndexPublished`, `KnowledgeRightsChanged` with affected document/version/index.

## Excludes
Normalizing wine measurements, vineyard activities, AI provider quotas, final decision ownership.

## MVP acceptance
OCR 1–2 books, import curated URLs, page-cited hybrid retrieval, enforce private/allowed-export policy, cache/rebuild embedding index independently.
