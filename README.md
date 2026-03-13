# Procedo

Procedo is a .NET workflow engine for YAML-defined pipelines with dependency-aware execution, plugin-based steps, persistence, resume, and observability.

It is designed for local automation, embedded workflow hosts, and operator-friendly execution from the command line.

## Why Procedo

- define workflows in YAML with `stages -> jobs -> steps`
- execute dependency-aware graphs with step outputs and expressions
- shape workflows with template-time `${{ if }}` / `${{ elseif }}` / `${{ else }}` / `${{ each }}`
- gate declared steps at runtime with `condition:`
- extend the runtime with plugins and app-registered custom steps
- persist runs locally, resume waiting workflows, and inspect run state
- validate workflows and runtime parameters before execution
- emit structured execution events to console, JSONL, or custom sinks

## Package overview

Recommended public packages:

- `Procedo.Engine` for workflow execution
- `Procedo.Hosting` for YAML loading, validation, and host composition
- `Procedo.Plugin.SDK` for plugin and step authoring contracts
- `Procedo.Plugin.System` for built-in `system.*` steps
- `Procedo.Extensions.DependencyInjection` for `IServiceCollection` integration

More detail:

- [Package Guide](./docs/PACKAGE_GUIDE.md)

## Framework support

Library projects multi-target:

- `net5.0`
- `net6.0`
- `net7.0`
- `net8.0`
- `net9.0`
- `net10.0`

## Quick start

Run a workflow with the reference CLI host:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml
```

Embed Procedo in your own app:

```csharp
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var yaml = await File.ReadAllTextAsync("examples/01_hello_echo.yaml");

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteYamlAsync(yaml);
Console.WriteLine(result.Success ? "Success" : result.Error);
```

Minimal workflow:

```yaml
name: hello_echo
version: 1

stages:
- stage: main
  jobs:
  - job: hello
    steps:
    - step: say_hello
      type: system.echo
      with:
        message: "Hello from Procedo"
```

Runtime-gated workflow:

```yaml
name: deploy_demo
version: 1

parameters:
  environment: prod

stages:
- stage: deploy
  jobs:
  - job: main
    steps:
    - step: announce
      type: system.echo
      condition: eq(params.environment, 'prod')
      with:
        message: "Deploying to production"
```

For a fuller walkthrough:

- [Getting Started In 5 Minutes](./docs/GETTING_STARTED.md)
- [Embedding Procedo](./docs/EMBEDDING_PROCEDO.md)
- [Control-Flow Recipes](./docs/CONTROL_FLOW_RECIPES.md)

## CLI examples

Inspect available commands:

```powershell
dotnet run --project src/Procedo.Runtime -- --help
```

Run with persistence:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --persist --state-dir .procedo/runs
```

Resume a waiting run:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume <runId> --resume-signal continue --state-dir .procedo/runs
```

Run a template workflow with runtime parameters:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

Emit execution events:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --events-console --events-json .procedo/events.jsonl
```

## Testing

```powershell
dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj -m:1
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj -m:1
dotnet test tests/Procedo.ContractTests/Procedo.ContractTests.csproj -m:1
```

## Current known limits

- single-node runtime only
- file-backed persistence only
- template workflows can inherit from one base template, but Procedo does not support arbitrary graph composition or fragment merging
- parameter constraints cover common practical checks, not the full JSON Schema feature set
- template-time `${{ each }}` currently targets array iteration only

## Examples

If you want to explore the repo quickly, start with one of these paths:

1. Use the umbrella launcher to list and run example apps and YAML suites.
2. Run a single workflow YAML with `Procedo.Runtime`.
3. Open one of the starter example apps if you want embedding patterns.

Example index:

- [Examples Catalog](./examples/README.md)

Starter example apps:

- `examples/Procedo.Example.Basic` shows direct parser, validator, and engine usage
- `examples/Procedo.Example.CustomSteps` shows delegate, DI-backed, and method-binding custom steps
- `examples/Procedo.Example.DependencyInjection` shows `IServiceCollection` integration
- `examples/Procedo.Example.Templates` shows template-driven workflows
- `examples/Procedo.Example.SecureRuntime` shows locked-down `system.*` execution
- `examples/Procedo.Example.WaitResume` shows persistence-backed wait/resume flow

Starter YAML workflows:

- `examples/01_hello_echo.yaml` is the smallest runnable workflow
- `examples/05_outputs_and_expressions.yaml` shows outputs and expression binding
- `condition:` supports runtime boolean expressions such as `eq(...)`, `and(...)`, and `contains(...)`
- `examples/16_persistence_resume_happy_path.yaml` shows persisted run state
- `examples/48_template_parameters_demo.yaml` shows templates, parameters, and workflow variables
- `examples/49_parameter_schema_validation_demo.yaml` shows richer parameter validation

Browse or execute examples from one entry point:

```powershell
dotnet run --project examples/Procedo.Example.Catalog -- --list
dotnet run --project examples/Procedo.Example.Catalog -- --run basic
dotnet run --project examples/Procedo.Example.Catalog -- --run catalogs
```

Run a single workflow directly:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## Documentation

- [CHANGELOG](./CHANGELOG.md)
- [License](./LICENSE)
- [Contributing Guide](./CONTRIBUTING.md)
- [Getting Started In 5 Minutes](./docs/GETTING_STARTED.md)
- [Package Guide](./docs/PACKAGE_GUIDE.md)
- [Embedding Procedo](./docs/EMBEDDING_PROCEDO.md)
- [Method Binding](./docs/METHOD_BINDING.md)
- [Templates](./docs/TEMPLATES.md)
- [Control-Flow Recipes](./docs/CONTROL_FLOW_RECIPES.md)
- [Security Model](./docs/SECURITY_MODEL.md)
- [Observability](./docs/OBSERVABILITY.md)
- [Persistence](./docs/PERSISTENCE.md)
- [Validation](./docs/VALIDATION.md)
- [Testing](./docs/TESTING.md)
- [Production Readiness Plan](./docs/PRODUCTION_READINESS.md)
- [Phase 1 Release Checklist](./docs/PHASE1_RELEASE_CHECKLIST.md)
- [Phase 2 Handoff](./docs/PHASE2_HANDOFF.md)
- [Runtime Compatibility Policy](./docs/API_COMPATIBILITY.md)
- [Runtime Runbook](./docs/RUNBOOK.md)
- [Troubleshooting](./docs/TROUBLESHOOTING.md)
- [Capacity Guidance](./docs/CAPACITY.md)
- [Plugin Authoring Contract](./docs/PLUGIN_AUTHORING.md)
- [Release Notes Template](./docs/RELEASE_NOTES_TEMPLATE.md)
- [Phase 1 Release Notes](./docs/RELEASE_NOTES_PHASE1.md)
- [Support Matrix](./docs/SUPPORT_MATRIX.md)
- [Known Limitations](./docs/KNOWN_LIMITATIONS.md)
- [Next Enhancements](docs/NEXT_ENHANCEMENTS.md)

