# Repository layout — Gate 1 proposal

One monorepo, modular monolith, a single runnable .NET host for MVP.

```text
wAInery/
├── README.md, AGENTS.md, .gitignore
├── docs/
│   ├── 00-governance/   # gates, agent parallelism, ownership, review
│   ├── 10-architecture/ # C4, runtime, dependency policy
│   ├── 20-domains/      # bounded contexts (Gate 2)
│   ├── 30-components/   # module LLDs (Gate 3)
│   ├── 40-contracts/    # prose contract specs
│   ├── 50-data/         # source-of-truth, persistence, migration
│   ├── 60-adrs/         # numbered architecture decisions
│   ├── 70-quality/      # evals, security, review findings
│   └── 80-delivery/     # implementation packages
├── contracts/           # versioned, machine-readable integration schemas
│   ├── events/
│   ├── api/
│   ├── tools/
│   └── examples/
├── src/
│   ├── Host/            # single local API + worker process
│   ├── BuildingBlocks/  # only smallest universally shared primitives
│   ├── Modules/         # one directory per bounded context after Gate 2
│   └── Adapters/        # provider-specific connectors, when warranted
├── tests/
│   ├── Architecture/
│   ├── Contracts/
│   ├── Integration/
│   ├── Modules/
│   └── Evaluation/
├── agents/
│   ├── assignments/     # one independently implementable work package
│   ├── handoffs/        # explicit interface change requests
│   ├── reviews/         # reviewer output and decisions
│   └── templates/
├── tools/               # optional OCR, ingestion and evaluation scripts
├── infra/
│   ├── local/           # low-cost docker/podman startup
│   └── cloud/           # deferred; no cloud commitment in MVP
├── scripts/             # validation, bootstrap, smoke tests
└── .github/workflows/    # future CI, lint, contracts checks
```

**Do not create actual bounded-context names here before Gate 2.** Module template will contain `Domain`, `Application`, `Infrastructure`, `Contracts` within each module boundary. A contract may later be compiled as a separate project for enforcement; the Gate 3 review will decide based on dependency graph and iteration costs.

The directory structure is deliberately lighter than a full microservice architecture. Empty future extension points have explanatory READMEs, not premature implementations.
