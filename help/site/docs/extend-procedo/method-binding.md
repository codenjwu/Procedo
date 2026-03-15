---
title: Method Binding
description: Register ordinary C# methods as Procedo step implementations with input and service binding.
sidebar_position: 3
---

Method binding lets you register ordinary C# methods as workflow steps without creating a full step class.

## Registration Example

```csharp
registry.RegisterMethod("custom.method_summary", (Func<string, string, PublishOptions, SummaryPayload>)BuildSummary);
```

## Supported Binding Sources

Current method binding supports:

- workflow inputs by parameter name
- input aliases with `[StepInput("...")]`
- POCO binding from flat or nested `with:` input
- explicit sources via:
  - `[FromStepContext]`
  - `[FromServices]`
  - `[FromLogger]`
  - `[FromCancellationToken]`

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.CustomSteps
```

## When To Use Method Binding

- low-ceremony app-local step logic
- signatures that map cleanly to one operation
- lightweight service and context injection

## Related Content

- [Create a Custom Step](./create-a-custom-step.md)
- [Embedding Procedo](../use-in-dotnet/embedding-procedo.md)
