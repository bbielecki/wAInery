# Runtime source tree

`Host`: single runnable API + scheduled/background workers in MVP. `Modules`: vertical slices aligned to bounded contexts once approved. `Adapters`: pluggable outside systems. `BuildingBlocks`: minimal primitives; modules should not couple through it to bypass explicit contracts.
