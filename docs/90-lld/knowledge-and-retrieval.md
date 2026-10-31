# LLD — Documents, private knowledge and retrieval

## Pipeline
`SourceReceipt` (book PDF or curated URL) → rights classification → local original SHA256 storage → parse/OCR into structured pages/sections → quality checks (page count, language, tables, figure captions, numerical units) → semantic chunks → embedding space version → lexical index → **atomic index publication**. Keep page/paragraph anchors for source citations. Docling is a batch CLI/Python adapter; not a permanent microservice. OCR failures quarantine a version and retain previous published index.

Chunking: start with section-bound fragments of ~400–900 tokens, maximum size cap, parent heading, original page range and short overlap; compare against 250/500/1000 in evaluation rather than assuming optimal. Multilingual embeddings selected after a PL/EN retrieval benchmark; local CPU small embedding model preferred if quality acceptable. New embedding model = new `embedding_space` and dual index; do not mix different vector spaces. No need ANN for ~400 pages initially.

## Retrieval algorithm
1. Build filter from subject tags (variety, type, phenological stage, style/process), geography similarity, publication currency and rights classes.
2. Lexical query using `tsvector`/normalization plus vector nearest neighbor (exact within matching model space).
3. Fuse with reciprocal rank fusion (`score=Σ1/(k+rank)`), dedupe near-identical passages; rerank locally/cheaply only if benchmark gain.
4. Return `EvidenceItem {documentId, documentVersionId, chunkId, pages, quoteSnippet, sourceUrl?, publicationDate?, rights, relevanceSignals}`. Assert each citation resolves to a retained source version; no hallucinated page numbers.
5. Build capped context, ideally source diversity and multiple viewpoints when advice contested. Prefer suitability to Polish conditions *alongside* scientific quality. Source from another climate may be explanatory but should carry transferability caveat.

## Rights and egress
`PRIVATE_COPYRIGHT` is local scanned content; derivatives inherit restricted class until review. Policy `DENY|REVIEW|ALLOW` enforced by code before API model request. The default is **local-only** text/embedding/OCR for copyrighted books; approved snippets can be sent to external inference only after explicit policy and service terms review. No hosted vector store containing whole book, no book pages in telemetry, commits or email. Distinguish public-readable website from reusable commercial content.

## Source-of-truth and update impacts
Source original immutable; `DocumentVersion` tracks parser and source hash. `KnowledgeIndexPublished` sends affected tags/varieties/phases/version; unknown tags trigger bounded audit, not global expensive recalculation by default. A new web article has provenance URL, capture time, site terms/license status, author/publication date (if available), and freshness policy. Imported third-party pages are treated as **data**, never executable prompt instructions.

## Test vectors
- Czech/Polish/Italian enology terms, synonyms Brix/Blg, phenological codes, spelling/diacritics.
- Multi-column scanned text, tables with dosage units, page attribution, contradictory advice, outdated plant-protection labels.
- Gold evidence Top-5 recall >= 85% (proposed benchmark target), permission tests, no leakage via logs and citations.
- Reject advice built from too few relevant sources or unsupported region extrapolation; signal `INSUFFICIENT_DATA` instead of made-up confidence.
