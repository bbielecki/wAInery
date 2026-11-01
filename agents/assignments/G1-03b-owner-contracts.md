# G1-03b work order / IO contract decision

Issue #20, milestone #10; Architect / Integration Owner; BR-08/09/10; budget 0.
Stage Independently reviewed; final fresh-main tests/CI pending.
Approved upstream: PRs 13/15/17/19. No concurrent owner persistence.

Allowlist: Vineyard/Winery/Observations Contracts, shared scoped identity/command
DTOs in BuildingBlocks, tests/Contracts and references, scoped handoff. No owner
implementation, migration, JSON schema or economic/rights policy change.

IO freezes new in-process seams (not HTTP DTO serialization): owner ResolveAsync
returns canonical UUID only after scoped opaque-ID lookup; read DTOs carry asOf
and knownAt; immutable arrays; write commands carry CommandContext and reject
workspace/owner mismatch. Draft proposal and explicit confirmation are separate
commands; confirmation requires proposal ID/token, performed time and actor.
Winery batch and vessel IDs remain distinct. Observation sum value preserves
numeric/text/boolean/categorical source value, zero/false and explicit units;
v1 wire remains NUMERIC/NARRATIVE, without adding enum members or new fields.
IO technical read guard: observation pages 1..500 records with an opaque cursor;
caller filters are copied before use. This is not a model/API spend policy.
Review amendment: Vineyard owns optional opaque WeatherLocationId profile binding,
registration/update/read and paged reverse lookup (location→block refs). Owner
validates the location through Weather public read port, not foreign tables or
Host mapping. Weather location read seam completion remains G1-03c.
Review amendment: explicit owner PrepareConfirmation returns a token bound to
proposal/subject/actor/verified performed time, without accepting performed work.
No token is returned by untrusted extraction/proposal. Future owner must check
authenticity, version, expiry/reuse and exact target/time at confirmation.
Descriptive confirmed read/proposal DTOs retain optional quantity/unit/material;
missing remains unknown and cannot authorize an actual chemical/physical action.
Registry/unit authorization, unknown-ID lookup, as-of filtering and token
verification must be tested against real owner implementations in later gates.

Contract tests first: compile RED then actual constructor/immutable value and
workspace failures, v1 synthetic fixture consumption. No mock port can prove
database/as-of behavior. Reuse contract-change and PR-verification skills.
Consumers: Connectivity/Decisions after merge; scoped handoff records pending
acknowledgement. Independent review, current-main rebase and CI before squash.
Gate 2 remains unauthorized. No transferable finding assumed in advance.
