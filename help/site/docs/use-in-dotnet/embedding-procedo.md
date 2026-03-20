---
title: Embedding Procedo
description: Embed Procedo in your own .NET application to load, validate, and execute workflows directly.
sidebar_position: 1
---

Embed Procedo when your application needs to execute YAML-defined workflows directly instead of shelling out to the CLI host.

## Recommended Package Set

Most embedding scenarios need:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System` if you want built-in `system.*` steps

Use `Procedo.Extensions.DependencyInjection` if your application already uses `IServiceCollection`.

## Minimal Host Example

```csharp
var yaml = await File.ReadAllTextAsync("examples/01_hello_echo.yaml");

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteYamlAsync(yaml);
```

## Validated Example App

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

## What Embedding Gives You

- YAML loading inside your app
- validation before execution
- custom step registration
- observability/event integration
- persistence and resume options in a host-managed runtime
- callback-driven resume through active wait queries and resume-by-wait-identity

## Validated Embedding Examples

The repository now includes richer host-facing examples beyond the minimal starter:

```powershell
dotnet run --project examples/Procedo.Example.Basic
dotnet run --project examples/Procedo.Example.CallbackResumeHost
dotnet run --project examples/Procedo.Example.AdvancedObservability
dotnet run --project examples/Procedo.Example.ParityRunner
dotnet run --project examples/Procedo.Example.PolicyHost
dotnet run --project examples/Procedo.Example.CustomResolverStore
```

## Related Content

- [ProcedoHostBuilder](./procedo-host-builder.md)
- [Callback-Driven Resume](./callback-driven-resume.md)
- [Dependency Injection Integration](../extend-procedo/dependency-injection-integration.md)
