# Ubiquitous language — agreed candidates, Gate 2

| Term | Meaning | Owner |
|---|---|---|
| `VineyardBlock` | Physically and agronomically meaningful management area with planted varieties | Vineyard |
| `VarietyPlanting` | Placement and share/count of grape variety in block (not independent global variety catalog) | Vineyard |
| `PhenologicalStage` | Accepted current growth phase of a block/plant cohort, with effective date | Vineyard |
| `VineyardActivity` | **Performed or planned** field action (spraying, pruning, sampling) with execution state | Vineyard |
| `WineBatch` | Traceable volume/identity of wine or must through processing; independent of vessel | Winery |
| `Vessel` | Physical tank/container; its contents may change | Winery |
| `WineryOperation` | Cellar action, e.g. pressing, transfer, racking, nutrient addition, with outcome | Winery |
| `Observation` | An immutable, timestamped measured or descriptive fact, potentially uncertain | Observations |
| `MeasurementType` | Definition of quantity, unit constraints, method expectations | Observations |
| `ForecastRun` | Snapshot of a weather provider forecast issued at a specific time | Weather |
| `Evidence` | Identifiable and authorized source span/fact with provenance | Knowledge |
| `Suitability` | Evidence applicability to the given location, variety, phase and climate, separate from scientific credibility | Knowledge / Decisions consumer |
| `Prediction` | Time-bounded estimate/assessment of future event or need, with inputs and uncertainty | Decisions |
| `RiskAssessment` | Estimate of likelihood/consequences of a possible problem; probability only when supported | Decisions |
| `ActionCandidate` | A proposed, permissible alternative or step with dependencies and trade-offs | Decisions |
| `DecisionPlan` | Versioned selection/ordering of action candidates with deadlines and rationale | Decisions |
| `DecisionFeedback` | User's disposition of plan; actual execution is confirmed in operational domain | Decisions |
| `Source` | Configured integration or acquisition mechanism (book import, HTTP provider, manual entry) | Connectivity |
| `KnowledgeDocument` | Immutable-versioned source document with IP rights and chunk lineage | Knowledge |
| `ModelInvocation` | Auditable, policy-compliant call to AI provider, not an accepted decision | AIExecution |

Key distinction: **Forecast** ≠ **Observation**; **Recommendation** ≠ **Executed operation**; **Prediction probability** ≠ **AI confidence**; **Sample in tank** ≠ **batch permanently tied to tank**; **Raw note** ≠ **verified operational fact**. Preserve Polish source vocabulary as aliases/tags; canonical API terms remain consistent and documented.
