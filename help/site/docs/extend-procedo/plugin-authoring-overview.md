---
title: Plugin Authoring Overview
description: Learn the main ways to extend Procedo with custom step implementations.
sidebar_position: 1
---

Procedo can be extended with custom step types that plug into the same runtime execution model as built-in steps.

## Core Contract

The base contract is:

```csharp
public interface IProcedoStep
{
    Task<StepResult> ExecuteAsync(StepContext context);
}
```

## Main Registration Modes

Procedo currently supports:

- direct `IProcedoStep` registration
- delegate registration
- DI-backed activation
- method binding

## When To Implement A Plugin

Add custom steps when:

- built-in `system.*` steps are not enough
- your workflow needs app-specific behavior
- you want reusable operational building blocks

## Good Plugin Practices

- respect `context.CancellationToken`
- keep outputs stable when downstream expressions depend on them
- prefer JSON-friendly output values
- use `context.Logger` for operational logging

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## Related Content

- [Create a Custom Step](./create-a-custom-step.md)
- [Method Binding](./method-binding.md)
