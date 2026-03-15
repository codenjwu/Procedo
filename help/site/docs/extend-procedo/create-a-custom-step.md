---
title: Create a Custom Step
description: Add a custom Procedo step through delegate, class-based, or DI-backed registration.
sidebar_position: 2
---

The fastest way to learn custom step registration is to use the repository example that demonstrates multiple registration modes in one host.

## Validated Example App

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## What It Demonstrates

The current example registers:

- a delegate-based step
- a DI-backed `IProcedoStep`
- method-bound steps with explicit binding attributes

## Delegate Example

```csharp
registry.Register("custom.delegate_hello", context => new StepResult
{
    Success = true,
    Outputs = new Dictionary<string, object>
    {
        ["greeting"] = $"Delegate hello, {context.Inputs["name"]}",
        ["name"] = context.Inputs["name"]
    }
});
```

## DI-backed Step Example

```csharp
registry.Register<DiHelloStep>("custom.di_hello");
```

## When To Choose Which

- use delegates for small app-local logic
- use `IProcedoStep` for larger or reusable behavior
- use DI-backed steps when the step needs services

## Related Content

- [Plugin Authoring Overview](./plugin-authoring-overview.md)
- [Dependency Injection Integration](./dependency-injection-integration.md)
