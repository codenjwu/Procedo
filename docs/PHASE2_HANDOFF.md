# Phase 2 Handoff

Use this document to quickly resume Procedo development in a new machine/session.

## Current status

- Phase 1 (single-node production baseline) is complete.
- Demo plugin support is in place and registered by default in runtime (`system.*` + `demo.*`).
- Complex examples are available and documented with expected outcomes.
- Demo unit and integration tests are added and validated on maintainer local environment.

Primary references:

- Phase 1 evidence: `docs/PHASE1_RELEASE_CHECKLIST.md`
- Production roadmap and Phase 2 TODO: `docs/PRODUCTION_READINESS.md`
- Change log snapshot: `CHANGELOG.md`
- Example scenarios: `examples/README.md`
- Plugin authoring contract: `docs/PLUGIN_AUTHORING.md`

## Phase 2 scope (agreed priorities)

## Integration pattern baseline

Procedo now includes a host/builder integration pattern for embedding in user applications:

- `src/Procedo.Engine/Hosting/ProcedoHostBuilder.cs`
- `src/Procedo.Engine/Hosting/ProcedoHost.cs`
- `src/Procedo.Engine/Hosting/ProcedoHostOptions.cs`

Recommended default for new integrations:

- Use `ProcedoHostBuilder` for options/configuration extensibility.
- Register plugins via `ConfigurePlugins(...)`.
- Configure execution/validation policies via builder methods.
- Add project-specific extension methods over the builder for reusable defaults.

Reference consumer apps:

- `examples/Procedo.Example.Basic` (direct parser + validator + engine)
- `examples/Procedo.Example.Extensible` (builder + extension methods)

For new sessions, prefer extending the builder pattern first unless a low-level engine-only use case is explicitly required.

Focus for next phase:

1. Persistence hardening
2. Security and isolation
3. Observability maturity

Deferred unless needed later:

- Distributed orchestration / multi-node scheduling

## Suggested first implementation task

Start with **Persistence hardening**:

- Add database-backed run state store abstraction implementation (for example SQLite first).
- Keep `IRunStateStore` contract unchanged for compatibility.
- Add migration/version metadata for run-state schema.
- Add tests for concurrent writes, crash recovery, and version compatibility.

## Ready-to-run validation commands

```powershell
# full tests

dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj -m:1
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj -m:1
dotnet test tests/Procedo.ContractTests/Procedo.ContractTests.csproj -m:1

# focused demo suites

dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj --filter "FullyQualifiedName~DemoPluginStepTests"
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj --filter "FullyQualifiedName~WorkflowDemoExamplesIntegrationTests"
```

## Notes for new AI sessions

Prompt template:

- "Read `docs/PHASE2_HANDOFF.md`, `docs/PRODUCTION_READINESS.md`, and `docs/PHASE1_RELEASE_CHECKLIST.md`. Propose implementation plan for Phase 2 persistence hardening and start coding."

Context notes:

- Repository may run fine locally even if some hosted/sandbox environments have SDK workload resolver issues.
- Prefer local machine test/build outputs as source of truth when sandbox behavior differs.
