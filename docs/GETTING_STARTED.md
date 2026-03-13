# Getting Started In 5 Minutes

This guide is the fastest way to run Procedo locally and understand the main entry points.

## 1. Build the repo

```powershell
dotnet build Procedo.sln
```

## 2. Run your first workflow

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml
```

That uses the reference CLI host in [src/Procedo.Runtime](/D:/Project/codenjwu/Procedo/src/Procedo.Runtime) and executes a small YAML workflow end to end.

If you want the smallest possible workflow, try:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

## 3. Run a workflow with dependencies

```powershell
dotnet run --project src/Procedo.Runtime -- examples/03_fan_out_fan_in.yaml
```

This shows dependency-aware execution across multiple steps.

## 4. See what embedding looks like

The smallest useful embedded host looks like this:

```csharp
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var yaml = await File.ReadAllTextAsync("examples/01_hello_echo.yaml");

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteYamlAsync(yaml);
```

If you want runnable examples instead of snippets:

Direct embedding example:

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

Custom-step registration example:

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

DI-based embedding example:

```powershell
dotnet run --project examples/Procedo.Example.DependencyInjection
```

## 5. Know the public package story

For most users, these are the packages that matter:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System`
- `Procedo.Extensions.DependencyInjection`

Package details:

- [Package Guide](/D:/Project/codenjwu/Procedo/docs/PACKAGE_GUIDE.md)

## 6. Know the main ways to explore examples

Use the umbrella launcher to browse or run example apps and YAML suites:

```powershell
dotnet run --project examples/Procedo.Example.Catalog -- --list
dotnet run --project examples/Procedo.Example.Catalog -- --run basic
dotnet run --project examples/Procedo.Example.Catalog -- --run catalogs
```

Or run a single workflow directly with the CLI host:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## 7. Next docs to read

Recommended order:

1. [Embedding Procedo](/D:/Project/codenjwu/Procedo/docs/EMBEDDING_PROCEDO.md)
2. [Method Binding](/D:/Project/codenjwu/Procedo/docs/METHOD_BINDING.md)
3. [Plugin Authoring Contract](/D:/Project/codenjwu/Procedo/docs/PLUGIN_AUTHORING.md)
4. [Control-Flow Recipes](/D:/Project/codenjwu/Procedo/docs/CONTROL_FLOW_RECIPES.md)
5. [Persistence](/D:/Project/codenjwu/Procedo/docs/PERSISTENCE.md)
6. [Security Model](/D:/Project/codenjwu/Procedo/docs/SECURITY_MODEL.md)

## 8. Useful test commands

```powershell
dotnet test tests/Procedo.UnitTests/Procedo.UnitTests.csproj -m:1
dotnet test tests/Procedo.IntegrationTests/Procedo.IntegrationTests.csproj -m:1
dotnet test tests/Procedo.ContractTests/Procedo.ContractTests.csproj -m:1
```

## 9. Pack the public package set

```powershell
./scripts/pack-nuget.ps1 -Profile public -IncludeSystemPlugin -Version 0.1.0
```

## Scope notes

Procedo Phase 1 is aimed at:

- single-node execution
- local persistence and resume
- embedded .NET host usage
- plugin and method-binding extensibility

It is not positioned as a distributed or multi-tenant orchestration platform.
