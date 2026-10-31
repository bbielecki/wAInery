# ADR-003 — Separate probability, evidence confidence and action utility

**Status:** Proposed, Gate 3.

Decision: Event probability and candidate effectiveness probability are optional structured model outputs with documented method, data basis, version and calibration status; AI text model does not manufacture percentages. Evidence confidence is qualitative; action utility/ranking is an *ordinal decision aid*, not a probability. Rank feasible action sets respecting dependencies/exclusivity and explicit no-action/measure-first baselines.

Tradeoff: MVP reports may omit requested percentages when evidence lacks statistical basis. This is deliberately preferred over unjustified precision. Calibration comes after enough local outcome data or external validated models.
