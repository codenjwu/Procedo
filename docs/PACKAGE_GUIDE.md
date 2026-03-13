# Package Guide

This guide defines the intended public NuGet package surface for Procedo Phase 1.

## Support matrix

Current library target frameworks:

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

## Phase 1 packaging goal

Procedo Phase 1 should present a small, understandable package surface for single-node workflow execution.

The goal is:

- clear package roles
- minimal end-user confusion
- stable entry points for embedding
- separation between public packages and implementation detail projects

## Recommended public packages

These are the recommended public packages for Phase 1:

### `Procedo.Engine`

Core execution engine package for embedding Procedo into an application.

Use when you need:

- workflow execution
- retries, waiting, resume, and runtime scheduling
- expression resolution used by runtime execution

Most users will pair this with `Procedo.Hosting`.

### `Procedo.Hosting`

High-level hosting package for YAML loading, validation, and ready-to-use host composition.

Use when you need:

- `ProcedoHostBuilder`
- `ProcedoHost`
- `WorkflowTemplateLoader`
- `YamlWorkflowParser`
- `ProcedoWorkflowValidator`
- default local host behavior with optional persistence and observability hooks

This is the main package most application embedders should start with.

### `Procedo.Plugin.SDK`

Plugin authoring and extension contract package.

Use when you need:

- `IProcedoStep`
- `StepContext`
- `StepResult`
- plugin registry abstractions
- delegate/DI/method-binding registration helpers
- method-binding attributes

Plugin authors and application embedders commonly need this package.

### `Procedo.Plugin.System`

Built-in `system.*` steps.

Use when you want out-of-box steps like:

- `system.echo`
- `system.http`
- file/directory operations
- hashing/encoding/archive helpers
- JSON/XML/CSV helpers
- guarded process execution

Recommended for most demo and local automation scenarios.

### `Procedo.Extensions.DependencyInjection`

Optional integration for `Microsoft.Extensions.DependencyInjection`.

Use when your application already uses:

- `IServiceCollection`
- `IServiceProvider`
- DI-based step activation and registration

## Implementation detail projects

These projects are internal repository organization units, not part of the intended public NuGet publish set:

- `Procedo.Core`
- `Procedo.Observability`
- `Procedo.Persistence`

They may still exist in the repository, but the public Phase 1 package story should not position them as end-user entry packages.

They may also appear in the main Visual Studio solution for developer visibility. That does not mean they are part of the public NuGet surface.

In this repository:

- solution membership is for developer ergonomics
- `IsPackable`, pack profile selection, and the pack script determine what becomes a published package
- internal projects now build normally in the main solution, but they remain non-public implementation-detail projects
- the public pack script bundles required internal DLLs into the supported public packages and rewrites package manifests so only the supported public package ids appear as NuGet dependencies

## Package selection by audience

### Most application embedders

Start with:

- `Procedo.Hosting`
- `Procedo.Engine`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`

Optional:

- `Procedo.Extensions.DependencyInjection`

### Plugin authors

Start with:

- `Procedo.Plugin.SDK`

Often also:

- `Procedo.Plugin.System` for reference implementations
- `Procedo.Hosting` for local embedding and validation/testing workflows

### Validation-only tools

Start with:

- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- optionally `Procedo.Plugin.System`

### DI-first applications

Start with:

- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Extensions.DependencyInjection`
- `Procedo.Plugin.System`

## Packaging profiles

The repository pack script supports these profiles:

### `minimal`

Packages a minimal engine-first set.

Example:

```powershell
./scripts/pack-nuget.ps1 -Profile minimal
```

### `public`

Packages the intended public Phase 1 surface.

Example:

```powershell
./scripts/pack-nuget.ps1 -Profile public -IncludeSystemPlugin -Version 0.1.0
```

### `all`

Packages all packable library projects.

Use mainly for internal validation, not as the default publishing story.

### `custom`

Packages an explicit set of projects and their project-reference closure.

## Recommended publish story

For Phase 1 documentation and onboarding, present this as the default user path:

1. `Procedo.Hosting` for most embedders
2. `Procedo.Engine` when consumers want lower-level execution control
3. `Procedo.Plugin.SDK` for plugin and custom step contracts
4. `Procedo.Plugin.System` for built-in `system.*` steps
5. optional `Procedo.Extensions.DependencyInjection` for DI-first applications

This keeps the story understandable and matches the majority of expected single-node consumers.

## Public contract summary

The contracts Procedo treats as important public surface for Phase 1 are:

- runtime CLI flags and behavior
- YAML workflow structure and supported template semantics
- execution event schema (`SchemaVersion`)
- plugin SDK types and registration patterns
- validation issue shape and runtime error-code shape

Lower-level internal project structure may still evolve more freely than the public entry points above.

## Known limitations summary

- Procedo is a single-node workflow engine in Phase 1.
- Persistence is local and file-backed.
- Template support is intentionally narrow.
- Parameter schemas provide pragmatic constraint checks, not full schema composition.

## Notes on current dependency shape

In practice today:

- the recommended public onboarding surface is `Procedo.Hosting`, `Procedo.Engine`, `Procedo.Plugin.SDK`, `Procedo.Plugin.System`, and optional `Procedo.Extensions.DependencyInjection`
- the public pack profile emits only those public packages

Further cleanup from here would be about internal repo organization rather than the published package list.

## Future cleanup candidates

Potential future improvements:

- reduce exposed package count further
- hide more implementation detail packages behind higher-level package boundaries
- add package READMEs per public package
- add package-specific samples to NuGet metadata/readme content
