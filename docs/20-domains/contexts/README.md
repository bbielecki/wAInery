# Proposed domain/context specifications

Gate 2 artifacts. These documents describe **ownership and boundaries**; component internals and executable APIs/events are deliberately deferred until Gate 3.

- Core: [Vineyard](Vineyard.md), [Winery](Winery.md), [Decisions](Decisions.md).
- Supporting: [Observations](Observations.md), [Weather](Weather.md), [Knowledge](Knowledge.md).
- Platform: [Connectivity](Connectivity.md), [AIExecution](AIExecution.md).

All directories in `src/Modules` are reserved ownership scopes, not implemented code. Do not create cross-context EF navigations or direct table writes.
