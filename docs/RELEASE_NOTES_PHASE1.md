# Procedo Phase 1 Release Notes

## Version

- Version: `0.1.0`
- Release date: `2026-03-12`

## Highlights

- Single-node workflow execution with YAML `stages -> jobs -> steps`
- Plugin-based runtime with built-in `system.*` steps and extensibility via `Procedo.Plugin.SDK`
- Local persistence, waiting runs, resume-by-run-id, inspection, and cleanup CLI flows
- Structured observability events with contract coverage and additive `SourcePath` attribution
- Template workflows and richer parameter schema validation for practical production embedding

## Added

- Runtime support for wait/resume, run inspection, bulk cleanup, and event sinks
- Richer parameter schema constraints including `allowed_values`, ranges, string length/pattern, array item typing, and required object fields
- Template loading with source attribution through run-level and step-level failure reporting
- `Procedo.Extensions.DependencyInjection` for DI-first host integration
- Expanded examples catalog covering system steps, templates, validation, persistence, observability, and resume scenarios

## Changed

- Public package metadata and packaging guidance aligned around the intended Phase 1 surface
- Example catalog normalized to the current expression syntax and reverified
- Pack script updated to package the public profile reliably

## Fixed

- Persistence/resume execution now seeds workflow parameters and variables consistently with non-persistent execution
- Template cloning now preserves nullable parameter defaults correctly
- Example workflows using outdated expression syntax were corrected and revalidated

## Compatibility

- DSL changes: additive only for Phase 1; existing required workflow fields remain stable
- Event schema changes: additive optional fields such as `SourcePath` preserve `SchemaVersion = 1`
- Runtime flag changes: additive operator flags for inspection and retention management

## Upgrade Notes

- Step outputs should be referenced as `${steps.<stepId>.outputs.<key>}`; `vars.*` is reserved for workflow variables
- Persistence/resume users can now rely on the same `${params.*}` and `${vars.*}` behavior across execution paths

## Validation

- Unit tests: green
- Integration tests: green
- Contract tests: green
