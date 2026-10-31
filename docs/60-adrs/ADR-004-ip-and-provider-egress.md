# ADR-004 — Local private knowledge and controlled provider egress

**Status:** Proposed, Gate 3.

Decision: source books, their OCR and restricted derivative chunks stored locally with lineage/rights labels. Knowledge filter enforces purpose and permitted retrieval; AIExecution independently enforces provider egress; context, provider policy and source class must all allow transmission. External LLM is optional; local-only source excerpts **never** leave without explicit user configuration/rights review.

Tradeoff: when external egress disallowed the remote model may reason over permitted operational facts/general knowledge only; higher quality local-book-grounded reasoning may require local model or separate informed opt-in. No protected content or secrets in Git, metrics or model logs. Public-source URL does not automatically grant reuse rights.
