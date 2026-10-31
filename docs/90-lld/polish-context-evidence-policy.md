# Evidence applicability to Polish vineyards and local winery practice

Status: **normative for MVP evidence selection**, not a legal opinion. Knowledge owner manages provenance/rights; Decisions owns applicability and advice constraints. See [retrieval](knowledge-and-retrieval.md), [safety](safety-and-recovery.md).

## Goal and bias avoidance

Books (1–2 in MVP) are only one knowledge source. Research from warm regions, general winemaking techniques, local practical notes and Polish sources may disagree. **Prefer applicable evidence, not merely Polish-language evidence**, and never treat origin country as a substitute for research quality or verified conditions. A locally collected observation is authoritative about *this vineyard's current conditions* but not proof of universal causality or safe efficacy.

## Evidence catalog fields

Each versioned evidence record needs: `sourceId`, `sourceVersion`, `documentId`, `pagesOrSection`, `publisher/author`, `publicationDate?`, `capturedAt`, `language`, `rightsClass`, `externalAiPolicy`, `topicTags`, `cultivarTags`, `phenologyTags`, `wineProcessTags`, `geoContext`, `climateEvidence`, `methodQuality`, `currencyRisk`, `transferabilityNotes`, `retractionOrSupersededStatus` and content checksum. Missing fields are `UNKNOWN`—never guessed.

- `geoContext`: country/region/altitude etc. only if source explicitly provides it; it is **not** the exact location of the user's vineyard.
- `climateEvidence`: available growing degree days, precipitation, humidity, frost exposure, continentality or other comparable evidence; optional with measured source.
- `methodQuality`: e.g. peer-reviewed study, public authority, instructional book, field note, forum. Keep original category + provenance, no universal numeric ranking by source type.
- `currencyRisk`: especially high for plant-protection labels, legal restrictions and technologies that change.
- `rightsClass` and `externalAiPolicy`: determine allowable retrieval and model egress **before** prompt construction.

## Retrieval and synthesis rules

1. Map decision to subject and time: cultivar, phase, block/lot, winery process, symptoms, known observations, proposed action, constraints, time horizon.
2. Fetch **operational facts from owner modules**, not from an embedding search. Local data is fresh state evidence, not copyrighted literature.
3. Retrieve indexed documentary evidence with hybrid search and page/URL citations. Include source diversity and disagreements; do not silently discard a contradictory reliable study.
4. Rank on **relevance + methodological reliability + transferability + freshness**. Implement transparent qualitative signals initially; tune weights only using judged scenarios.
5. Annotate a transfer gap: climate/cultivar/management mismatch; unknown local phenological stage; non-equivalent wine style; outdated label; method not demonstrated in Polish conditions.
6. If the gap affects a high-risk decision, do not produce false precision: recommend data collection, careful verification or escalation. Explicitly distinguish `scientifically_plausible` from `locally_validated`.
7. For contentious treatments surface alternatives and downside; keep exact quotation limited and rights-compliant. No wholesale copy of book sections.

## Polish-specific checks

| Topic | Mandatory decision context | Fail-safe if missing |
|---|---|---|
| Late-spring frost | Actual observed phase, block microclimate, timing, available protection | Ask for local observation / monitoring; avoid exact hour certainty |
| Fungal diseases | Symptoms or measured/modelled infection conditions, cultivar susceptibility, recent treatments | Reassess risk; never infer legal product/dose from generic literature |
| Plant-protection treatment | Current Polish product authorization, active label and crop/use scope, safety/interval rules | `POLICY_BLOCKED` for specific use until verified |
| Harvest | Real ripeness measurements, site weather, fruit state, stylistic goal | Seek samples; avoid generic date from warmer regions |
| Fermentation | Wine type, yeast, temperature and sugar history, nutrient/acid/SO2 data when relevant | Request measurement; do not invent dosage |
| Vessel/wine occupancy | Event times for transfer, current vessel vs actual batch at observed timestamp | Mark ambiguous readings unassigned |

## Source update and rights policy

- MVP web collection is **curated URLs**, capture timestamps and explicit provenance, not unconstrained browsing or a crawler.
- Source change can invalidate plans by tagged topic/cultivar/phase even when the old plan did not cite the new document. Unknown tags trigger a *bounded* coverage audit, not endless paid reanalysis.
- Copyrighted scans and OCR remain private and outside Git. Derivative chunks inherit restrictions until reviewed. For external LLMs, `DENY` remains default unless explicit source-level approval exists under applicable terms.
- A public URL is **not** evidence of commercial redistribution permission. Future public product has a separate approved knowledge workspace/index.

## Review fixtures

- Two valid studies contradict each other; show why local applicability differs and mark unresolved point.
- An online article omits climate/cultivar; answer must carry limitation rather than invent location.
- Polish forum anecdote conflicts with replicated evidence; show both provenance and choose appropriately justified interpretation.
- Label/product regulation is outdated; block explicit treatment recommendation without current authoritative check.
- A private book supports an answer but egress is `DENY`; no private excerpt is placed in external model request/log.

**Definition of done:** source provenance preserved at chunk level; answer cites actual resolvable page/URL; correct locality caveat; private text never leaves approved boundary; expert-reviewed transferability cases appear in the evaluation set.
