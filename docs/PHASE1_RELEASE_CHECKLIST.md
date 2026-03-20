# Phase 1 Release Checklist

This checklist is the working reference for Procedo Phase 1 release readiness.

Scope for Phase 1:

- single-node machine execution only
- no distributed orchestration
- production-capable local workflow engine embedding story
- stable core engine and plugin model for early adopters
- practical YAML flow/control and expression support for common pipeline scenarios

## Release goal

Phase 1 should deliver a reliable, documented, packageable workflow engine for local execution with:

- YAML workflow execution
- plugin-based extensibility
- local persistence and resume
- structured observability
- strong embedding story for .NET applications

## A. Runtime hardening

Required:

- [x] Define safe defaults for `system.process_run`
- [x] Add executable/path allowlist policy
- [x] Add file operation guardrails for allowed working directories
- [x] Improve startup validation for host/config/persistence setup
- [x] Standardize runtime error codes and failure categories
- [x] Improve user-facing diagnostics for step failures and misconfiguration

Notes:

- For Phase 1, focus on local safety and predictable behavior rather than full sandbox isolation.

## B. Persistence hardening

Required:

- [x] Define atomic write behavior for run-state files
- [x] Detect and handle corrupted or incomplete persisted state
- [x] Add persistence state version field for future compatibility
- [x] Document resume semantics and recovery edge cases
- [x] Add persistence troubleshooting guidance
- [x] Add tests for corruption and recovery scenarios

Notes:

- Persistence is a critical Phase 1 reliability feature because local resume is part of the core value proposition.
- Persisted execution now follows the same retry, timeout, `continue_on_error`, and `max_parallelism` policies as non-persisted execution.

## C. Lightweight security model

Required:

- [x] Document trusted vs untrusted workflow usage assumptions
- [x] Add masking guidance for secrets in logs/events
- [x] Add policy options for risky `system.*` steps
- [x] Clarify local-machine risk boundaries in docs
- [x] Add tests for blocked/restricted execution scenarios

Notes:

- Phase 1 does not require full sandboxing, but it does require guardrails and clear documentation.

## D. Basic observability readiness

Required:

- [x] Document structured event schema
- [x] Ensure event contracts are stable enough for Phase 1 consumers
- [x] Add local troubleshooting guidance using logs/events
- [x] Add tests for failure diagnostics and event consistency

Notes:

- Advanced tracing/metrics can wait. Phase 1 needs good local diagnostics first.
- Resumed runs now distinguish replayed completed work from true skipped steps in structured events.

## E. Configuration model

Required:

- [x] Define official Phase 1 configuration surface
- [x] Document strongly typed host/configuration options
- [x] Document precedence rules clearly
- [x] Add startup validation for invalid config combinations
- [x] Separate execution policy config from security policy config
- [x] Add embedding examples for common config patterns

Recommended precedence:

- defaults < config file < environment variables < CLI/app overrides

## E2. DSL flow/control and expressions

Required:

- [x] Add template-time conditional insertion syntax for `${{ if }}`
- [x] Add template-time conditional insertion syntax for `${{ elseif }}` and `${{ else }}`
- [x] Add template-time iteration syntax for `${{ each }}` (array iteration)
- [x] Add expression/condition functions for `eq`, `ne`, `and`, `or`, `not`
- [x] Add expression/condition functions for `contains`, `startsWith`, `endsWith`, `in`, `format`
- [x] Define evaluation timing clearly for template-time expansion vs runtime conditions
- [x] Document supported syntax, escaping, and examples for CLI/host users
- [x] Add validation errors for unsupported or malformed control/expression syntax
- [x] Add runtime step-level `condition:` evaluation

Notes:

- The goal is practical Azure-Pipelines-style authoring support for common branching and iteration scenarios, not a full general-purpose programming language.
- Phase 1 should keep this deterministic and predictable for single-node execution.

## F. Packaging and product surface

Required:

- [x] Finalize official public NuGet package list
- [x] Mark which projects are public packages vs examples/internal implementation details
- [x] Align package descriptions and metadata
- [x] Document package selection guidance for users
- [x] Ensure packaging scripts reflect the intended public package surface
- [x] Review package dependencies for unnecessary exposure

Recommended Phase 1 public package set:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

## G. Documentation

Required:

- [x] Add `METHOD_BINDING.md`
- [x] Add `PACKAGE_GUIDE.md`
- [x] Add `SECURITY_MODEL.md`
- [x] Add `PERSISTENCE.md`
- [x] Add `EXAMPLE_STRATEGY.md`
- [x] Add `TEST_STRATEGY.md`
- [x] Add `EXAMPLE_AND_TEST_BACKLOG.md`
- [x] Add Phase 1 known limitations section
- [x] Add recommended production usage guidance
- [x] Improve examples index by scenario and audience

Already present and should be kept current:

- `EMBEDDING_PROCEDO.md`
- `PLUGIN_AUTHORING.md`
- `CHANGELOG.md`

## H. More testing

Required:

- [x] Add cross-framework smoke tests for supported target frameworks
- [x] Add public API contract tests for extensibility surface
- [x] Add negative tests for risky `system.*` steps
- [x] Add persistence corruption/recovery tests
- [x] Add configuration validation/startup validation tests
- [x] Add packaging smoke tests
- [x] Add compatibility tests for method binding behavior
- [x] Add compatibility tests for `${{ if }}` / `${{ elseif }}` / `${{ else }}` expansion behavior
- [x] Add compatibility tests for `${{ each }}` expansion behavior
- [x] Add unit/integration tests for expression functions and runtime condition evaluation

Notes:

- Protecting public extension behavior is important now that delegate/DI/method-binding support exists.
- Examples should be treated as executable product assets, with catalog governance and golden scenario coverage continuing after the core Phase 1 engine release.
- The executable example program now includes dedicated null, parity, callback-resume, control-flow, composition, and scenario-pack golden coverage through examples `80` to `86`.

## I. Backward compatibility policy

Required:

- [x] Define stable Phase 1 contracts
- [x] Define experimental/evolving contracts
- [x] Define versioning and deprecation policy
- [x] Document compatibility guarantees for public APIs and workflow model

Recommended stable contracts:

- workflow stage/job/step model
- `IProcedoStep`
- `StepContext`
- `StepResult`
- plugin registry collision behavior
- host builder core behavior

Likely evolving contracts:

- method binding conventions
- DI helper ergonomics
- some `system.*` security-sensitive behavior

## J. Release polish

Required:

- [x] Review NuGet metadata for all public packages
- [x] Ensure `CHANGELOG.md` is current
- [x] Add Phase 1 release notes
- [x] Add target framework support matrix
- [x] Add known issues / limitations page or section
- [x] Ensure docs and examples match current code
- [x] Add a 5-minute getting started path
- [x] Review naming consistency across packages/docs/examples

## Suggested execution order

Recommended order for completing the remaining Phase 1 work:

1. Configuration model
2. Packaging and product surface
3. Documentation completion
4. More testing
5. Backward compatibility policy
6. Release polish
7. Remaining runtime hardening, security, and persistence gaps exposed during the above work

## Immediate next tasks

This checklist is now mostly a verification record rather than an implementation todo list.

If additional Phase 1 polish is needed, focus on:

1. final package inspection before publication
2. version/changelog confirmation for the release candidate
3. optional additional example polish if desired

## Remaining likely blockers

Most of the Phase 1 foundation is now complete. The main remaining blockers are:

- verify final package/readme polish before public publication

## Phase 1 completion criteria

Phase 1 is ready when:

- core public package surface is finalized
- embedding path is clearly documented
- custom step registration modes are documented and tested
- persistence behavior is documented and hardened for local use
- risky local execution features have documented guardrails
- practical flow/control and expression syntax is implemented, documented, and tested
- public API stability expectations are documented
- examples, docs, and package metadata are aligned
- release notes/changelog/support matrix are up to date

