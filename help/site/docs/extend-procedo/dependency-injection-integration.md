---
title: Dependency Injection Integration
description: Use the Procedo DI package to register hosts and steps through IServiceCollection.
sidebar_position: 4
---

If your application already uses `Microsoft.Extensions.DependencyInjection`, use the `Procedo.Extensions.DependencyInjection` package to register Procedo through `IServiceCollection`.

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.DependencyInjection
```

## What The Example Shows

- `services.AddProcedo()`
- system plugin registration
- DI-backed custom step registration
- delegate step registration
- method binding registration
- resolving a ready-to-use `ProcedoHost` from the service provider

## Example Pattern

```csharp
services.AddProcedo()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .RegisterStep<DiGreetingStep>("custom.di_greeting")
    .RegisterStep("custom.delegate_suffix", static context => new StepResult { ... })
    .RegisterMethod("custom.compose_message", (Func<string, string, ComposedMessage>)ComposeMessage);
```

## Related Content

- [Embedding Procedo](../use-in-dotnet/embedding-procedo.md)
- [Create a Custom Step](./create-a-custom-step.md)
