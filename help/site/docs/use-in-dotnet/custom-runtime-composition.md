---
title: Custom Runtime Composition
description: Compose a host with stricter validation, event sinks, retry policy, and custom plugin sets.
sidebar_position: 4
---

As your host grows, you will often want more than a minimal builder setup.

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.Extensible
```

## What The Example Shows

- adding both system and demo plugins
- enabling strict validation
- enabling console events
- customizing execution defaults such as retries and parallelism

## Example Pattern

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseStrictValidation()
    .UseConsoleEvents()
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 4;
        execution.DefaultStepRetries = 2;
        execution.RetryInitialBackoffMs = 50;
        execution.RetryMaxBackoffMs = 250;
    })
    .Build();
```

## When To Use This Pattern

- production-oriented embedded hosts
- hosts that need consistent execution policy
- apps that want observability integrated from the start

## Related Content

- [ProcedoHostBuilder](./procedo-host-builder.md)
- [Validation](../run-and-operate/validation.md)
