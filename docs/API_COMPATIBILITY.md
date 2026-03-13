# Runtime Compatibility Policy

Procedo Phase 1 compatibility policy for the single-node runtime, embedding surface, and CLI behavior.

## Stable contracts for Phase 1

The following should be treated as stable for Phase 1 consumers unless a future major version explicitly says otherwise:

- workflow stage/job/step structure
- plugin contract via `IProcedoStep`
- `StepContext`
- `StepResult`
- plugin registry duplicate-registration semantics
- core `ProcedoHostBuilder` behavior
- `ProcedoHost.ExecuteYamlAsync(...)`
- `ProcedoHost.ExecuteWorkflowAsync(...)`
- runtime error code family (`PRxxx`)

## Evolving contracts for Phase 1

The following are available and supported, but may still change more quickly than the stable core contract:

- method-binding conventions and ergonomics
- DI helper/builder ergonomics in `Procedo.Extensions.DependencyInjection`
- risky `system.*` operational guardrails and policy controls
- some persistence operational semantics while hardening work continues

Consumers should avoid taking strict dependencies on incidental details of those evolving areas until they are explicitly promoted as fully stable.

## Package surface

Phase 1 public package guidance is centered on:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

Package ownership or internal assembly layout may evolve, but the supported public package story should remain aligned with that set unless release notes say otherwise.

## DSL compatibility

- Patch versions (`x.y.z`) do not remove existing DSL fields.
- Minor versions (`x.y`) may add optional fields.
- Existing required fields (`name`, `version`, `stages/jobs/steps`, `type`) remain stable.
- Current Phase 1 optional flow/control fields such as `condition:` and template-time `${{ if }}` / `${{ elseif }}` / `${{ else }}` / `${{ each }}` are part of the supported DSL surface.
- Deprecated fields are supported for at least one minor version before removal.

## Method-binding compatibility

- Existing method-binding features are supported for Phase 1 use.
- Additive features such as new attributes or new bindable sources may be introduced in minor releases.
- Binding diagnostics may become more detailed over time, but should not become less informative.
- Avoid depending on exact exception-message wording beyond the documented semantics.

## Execution event compatibility

- `ExecutionEvent.SchemaVersion` identifies event schema generation.
- Additive changes (new optional fields) do not require immediate schema bump.
- Breaking changes require schema version increment and documented migration notes.
- Consumers should ignore unknown fields for forward compatibility.

This means additive fields such as `SourcePath` are expected to appear over time without breaking compliant consumers.

## Runtime flag compatibility

- Existing CLI flags keep behavior within a major version.
- New flags are additive and optional.
- Behavior-changing defaults are only introduced in major versions.

## Error code compatibility

- Runtime failure codes (`PRxxx`) are stable once released.
- This includes both execution failures (`PR100` range) and CLI/runtime setup failures (`PR200` range).
- New error codes may be added; existing codes are not repurposed.

## Deprecation policy

- Deprecations should be documented in the changelog and relevant docs.
- Public stable APIs should not be removed in patch releases.
- Removal of a stable API should occur only in a major version after prior deprecation notice.
