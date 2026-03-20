# Changelog

All notable changes to Procedo are documented in this file.

The format loosely follows Keep a Changelog and uses semantic versioning.

## [Unreleased]

## [1.0.0-rc1] - 2026-03-20

### Added

- Runtime cleanup flag `--delete-waiting-older-than <timespan>` for waiting-run retention control.
- Richer workflow parameter schema support: `allowed_values`, numeric min/max, string length/pattern, array item typing, and required object properties.
- Validation/parser/unit coverage for richer parameter schema handling and waiting-run cleanup selection.
- Demo plugin package (`Procedo.Plugin.Demo`) with `demo.flaky`, `demo.sleep`, `demo.fail`, `demo.fail_once`, `demo.cancel`, `demo.quality`, and `demo.score` step types.
- Runtime now registers demo plugin steps by default for advanced example workflows.
- Expanded examples catalog and outcome documentation for demo scenarios (`examples/README.md`).
- Runtime execution policy controls (max parallelism, retries, timeout defaults, continue-on-error).
- Runtime config layering support: defaults < JSON config < environment variables < CLI.
- Runtime error code model (`PRxxx`) added to workflow run results.
- Runtime CLI help (`--help`/`-h`) with full option reference.
- Sample runtime config file: `procedo.runtime.json`.
- Phase 1 release evidence checklist: `docs/PHASE1_RELEASE_CHECKLIST.md`.
- Additional production docs: troubleshooting, capacity guidance, and plugin authoring contract.
- `Procedo.Extensions.DependencyInjection` package for `IServiceCollection`-based host/step registration.
- Method binding enhancements: input aliases, flat and nested POCO binding, explicit source attributes, and clearer binding diagnostics.
- Public package surface guide: `docs/PACKAGE_GUIDE.md`.
- Method binding guide: `docs/METHOD_BINDING.md`.
- Security model guide for trusted single-node usage: `docs/SECURITY_MODEL.md`.
- Persistence guide for local resume semantics: `docs/PERSISTENCE.md`.
- Persistence hardening for local run-state files: schema version stamping, legacy schema compatibility, atomic temp-file replacement writes, corrupted-state detection, and persisted waiting metadata for pause/resume workflows.
- Lightweight system-step security guardrails: `SystemPluginSecurityOptions`, runtime config/env support, path-root restrictions, HTTP host allowlists, and executable allowlists for risky built-in steps.
- New pack profile `public` in `scripts/pack-nuget.ps1` for intended Phase 1 public package publishing.
- Public pack profile now requires an explicit `-Version` and includes aligned repository/readme metadata for produced packages.
- Locked-down secure runtime example project demonstrating `SystemPluginSecurityOptions` with allowed and blocked workflows.
- Suppressed unsupported-target-framework build noise for `Procedo.Extensions.DependencyInjection` so multi-target test/build output stays clean.
- Structured execution event redaction for wait/resume payload branches and common sensitive keys, plus runtime cleanup command support via `--delete-run <runId>` and updated operator runbook guidance.
- Added workflow parameters, workflow-level variables, and narrow file-based template loading with runtime `--param key=value` support.
- Added template authoring guide, integration coverage, and runnable template example project.
- Callback-driven resume support with active wait queries, resume-by-wait-identity, deterministic duplicate handling, workflow snapshots, and concurrency-safe local file-store behavior.
- Public callback-resume models and host APIs:
  - `ActiveWaitState`
  - `WaitingRunQuery`
  - `ResumeWaitingRunRequest`
  - `WaitingRunMatchBehavior`
- Store capability interfaces for additive callback-resume adoption:
  - `IWaitingRunQueryStore`
  - `IConditionalRunStateStore`
  - `IWorkflowDefinitionResolver`
- Persisted execution parity hardening so persisted and non-persisted runs share retry, timeout, `continue_on_error`, and `max_parallelism` semantics.
- Runtime/control-flow expansion with template-time `${{ if }}`, `${{ elseif }}`, `${{ else }}`, array-only `${{ each }}`, runtime `condition:`, and practical expression functions.
- Example strategy, test strategy, and example/test backlog docs with executable example-governance coverage.
- New executable example packs covering null semantics, persistence parity, callback-driven resume, control-flow matrices, composition scenarios, and real-world scenario packs through `examples/86_model_promotion_governance_demo.yaml`.
- New embedding examples:
  - `Procedo.Example.CallbackResumeHost`
  - `Procedo.Example.AdvancedObservability`
  - `Procedo.Example.ParityRunner`
  - `Procedo.Example.PolicyHost`
  - `Procedo.Example.CustomResolverStore`

### Docs

- Expanded root README with official package surface and documentation map.
- Added embedding guide (`docs/EMBEDDING_PROCEDO.md`).
- Added a formal Phase 1 production-readiness checklist and Phase 2 roadmap (`docs/PRODUCTION_READINESS.md`).
- Refreshed validation, persistence, runbook, templates, observability, and package guidance to match current runtime/operator capabilities.
- Added callback-driven resume requirements/backlog docs and an architecture hardening backlog.
- Added example strategy, test strategy, and example/test backlog docs.

### Release evidence snapshot

- Phase 1 release gate evidence: `docs/PHASE1_RELEASE_CHECKLIST.md`
- Production readiness plan + Phase 2 TODO checklist: `docs/PRODUCTION_READINESS.md`
- Package surface guidance: `docs/PACKAGE_GUIDE.md`
- Embedding guidance: `docs/EMBEDDING_PROCEDO.md`
- Method binding guidance: `docs/METHOD_BINDING.md`
- Security model guidance: `docs/SECURITY_MODEL.md`
- Persistence guidance: `docs/PERSISTENCE.md`
- Plugin authoring contract and reference implementations: `docs/PLUGIN_AUTHORING.md`
- Examples catalog with expected demo outcomes: `examples/README.md`

### Testing

- Added comprehensive demo plugin unit tests covering per-step behavior, state semantics, cancellation, and output contracts.
- Added integration tests executing advanced demo YAML workflows (retry, timeout, continue-on-error, persistence-resume, end-to-end).
- Added unit tests for DI integration, registry collision behavior, and advanced method-binding behaviors.
- Added cross-target contract smoke tests for the public Phase 1 package surface (`Procedo.Engine`, `Procedo.Plugin.SDK`, `Procedo.Plugin.System`, `Procedo.Validation`, `Procedo.Extensions.DependencyInjection`) on `net6.0`, `net8.0`, and `net10.0`.
- Added cross-target public extensibility contract tests covering SDK defaults, registry duplicate semantics, delegate registration, DI-backed step activation, and simple method registration behavior.
- Added wait/resume persistence and integration coverage for persisted waiting state, invalid resume handling, and resumed downstream execution.
- Added callback-resume integration and contract coverage, including active wait querying, repeated wait cycles, and workflow snapshot safety.
- Added persisted-vs-non-persisted parity integration coverage.
- Added null-semantics, control-flow matrix, composition golden, scenario golden, catalog governance, and embedding project smoke coverage.

## [0.1.0] - 2026-03-10

### Added

- Initial clean architecture engine/library layout and solution structure.
- YAML parser supporting `stages -> jobs -> steps` workflow hierarchy.
- DAG graph building and scheduler honoring `depends_on`.
- Plugin model via `IProcedoStep` and registry-based step resolution.
- Runtime console host (`Procedo.Runtime`) for local execution.
- Generic single-node wait/resume engine support with persisted `Waiting` run/step states, `ResumeRequest`, and `WaitDescriptor`.
- Built-in `system.wait_signal`, `system.wait_until`, and `system.wait_file` steps, plus runnable wait/resume example projects.
- Structured observability wait/resume metadata (`WaitType`, `WaitKey`, `SignalType`) and event coverage for `StepWaiting`, `RunWaiting`, and `RunResumed`.
- Runtime support for listing persisted waiting runs via `--list-waiting` and resuming them from the CLI with `--resume-signal` / `--resume-payload-json`.
- Local file-backed persistence for run/step state (`WorkflowRunState`, `StepRunState`).
- Resume execution by `runId`, including completed-step skipping and output rehydration.
- Expression resolver (`${steps.<id>.outputs.<key>}`, `${vars.<name>}`) with nested object/list support.
- Validation component with rules for required fields, duplicates, dependency integrity, cycle detection, step type format, plugin resolution, and expression reference safety.
- Validation modes: permissive and strict (`--strict-validation`).
- Structured observability event model:
  - `RunStarted`, `RunCompleted`, `RunFailed`
  - `StepStarted`, `StepCompleted`, `StepFailed`, `StepSkipped`
- Pluggable observability sinks:
  - `ConsoleExecutionEventSink`
  - `JsonFileExecutionEventSink`
  - `CompositeExecutionEventSink`
  - `NullExecutionEventSink`
- Event schema version field (`SchemaVersion = 1`).
- Contract/compatibility test coverage including cross-target (`net6.0`, `net8.0`, `net10.0`).

### Changed

- Engine now resolves expressions before each step execution.
- Engine emits structured execution events across normal and persistence-resume paths.
- Runtime supports event sink CLI flags:
  - `--events-console`
  - `--events-json <path>`
- JSON file event sink hardened for concurrent writes.
- Publisher and composite sink isolate sink failures from workflow execution.

### Testing

- Extensive unit, integration, stress, and contract tests.
- Golden snapshot tests for serialized event schema.
- Backward compatibility tests for legacy event payloads.
- Unknown-field tolerance tests for forward compatibility.













