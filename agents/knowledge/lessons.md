# Verified engineering lessons

This page starts intentionally without claims of implementation experience. The approved wAInery LLD and current CI demonstrate architecture documentation and build checks, **not** measured RAG quality or production behavior.

## Entry template

### LES-YYYY-NNN — Short, searchable problem title

- **Status:** Candidate / Verified / Superseded
- **Affected modules and business requirement:** e.g. Weather, Decisions / BR-xx
- **Problem and trigger:** what actually failed or was unexpectedly costly
- **Reproduction:** synthetic fixture, command or failing test (no user/private data)
- **Root cause (supported evidence):** links to GitHub PR/commit/test output
- **Resolution:** concise algorithm or change and its limits
- **Regression proof:** positive and negative test IDs; CI link
- **Consumers informed:** modules/agents/PR handoffs
- **Consider skill?** repeated frequency, expected savings, proposed owner
- **Superseded by:** ADR, canonical contract or newer lesson if applicable

Don't record unfounded speculation, chat transcripts, raw prompts, copyrighted book passages, external model contexts, secrets, or task progress already captured in GitHub. If a correction changes a design invariant, update owner documentation/ADR instead of letting this page become a competing source of truth.
