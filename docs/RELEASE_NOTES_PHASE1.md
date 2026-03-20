# Procedo Phase 1 Release Notes

## Version

- Version: `1.0.0-rc1`
- Release date: `2026-03-20`

## Highlights

- Single-node workflow execution with YAML `stages -> jobs -> steps`
- Plugin-based runtime with built-in `system.*` steps and extensibility via `Procedo.Plugin.SDK`
- Local persistence, active wait queries, resume-by-run-id, callback-driven resume, inspection, and cleanup flows
- Structured observability events with replay-aware resume semantics and contract coverage
- Template workflows, runtime `condition:`, `${{ if }}` / `${{ elseif }}` / `${{ else }}` / `${{ each }}`, and richer parameter schema validation
- Broad executable example catalog with golden scenario coverage and richer embedding projects

## Added

- Runtime support for wait/resume, run inspection, bulk cleanup, and event sinks
- Generic callback-driven resume support with active wait queries, resume-by-wait-identity, duplicate-match rules, workflow snapshots, and concurrency-safe local persistence
- Richer parameter schema constraints including `allowed_values`, ranges, string length/pattern, array item typing, and required object fields
- Template loading with source attribution through run-level and step-level failure reporting
- Template-time control flow: `${{ if }}`, `${{ elseif }}`, `${{ else }}`, and array-only `${{ each }}`
- Runtime `condition:` support and practical expression/condition functions: `eq`, `ne`, `and`, `or`, `not`, `contains`, `startsWith`, `endsWith`, `in`, and `format`
- `Procedo.Extensions.DependencyInjection` for DI-first host integration
- Expanded examples catalog covering null semantics, persistence parity, callback-driven resume, control-flow matrices, composition packs, scenario packs, and richer embedding examples
- New embedding examples:
  - `Procedo.Example.CallbackResumeHost`
  - `Procedo.Example.AdvancedObservability`
  - `Procedo.Example.ParityRunner`
  - `Procedo.Example.PolicyHost`
  - `Procedo.Example.CustomResolverStore`

## Changed

- Public package metadata and packaging guidance aligned around the intended Phase 1 surface
- Example catalog normalized to the current expression syntax and reverified
- Pack script updated to package the public profile reliably
- Persisted execution now follows the same retry, timeout, `continue_on_error`, and `max_parallelism` policies as non-persisted execution
- Embedding examples now expose configurable CLI surfaces instead of hidden scenario-only assumptions

## Fixed

- Persistence/resume execution now seeds workflow parameters and variables consistently with non-persistent execution
- Template cloning now preserves nullable parameter defaults correctly
- Example workflows using outdated expression syntax were corrected and revalidated
- File-backed persistence now preserves workflow snapshots for drift-safe callback-driven resume
- Resume replay semantics now distinguish replayed completed work from true skipped steps in structured events

## Compatibility

- DSL changes: additive only for Phase 1; existing required workflow fields remain stable
- Event schema changes: additive optional fields such as `SourcePath` preserve `SchemaVersion = 1`
- Runtime flag changes: additive operator flags for inspection, retention management, and waiting-run operations
- Custom run-state stores remain compatible through `IRunStateStore`; waiting-run queries and conditional-save semantics are opt-in via capability interfaces

## Upgrade Notes

- Step outputs should be referenced as `${steps.<stepId>.outputs.<key>}`; `vars.*` is reserved for workflow variables
- Persistence/resume users can now rely on the same `${params.*}` and `${vars.*}` behavior across execution paths
- Template-time `${{ each }}` is intentionally array-only in Phase 1
- Callback-driven resume requires persisted workflow snapshots for automatic workflow reconstruction

## Validation

- Unit tests: `250/250 passed`
- Integration tests: `145/145 passed`
- Contract tests: `57/57 passed` across `net6.0`, `net8.0`, and `net10.0`
