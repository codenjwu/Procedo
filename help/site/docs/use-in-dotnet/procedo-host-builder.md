---
title: ProcedoHostBuilder
description: Use ProcedoHostBuilder to configure plugins, execution, validation, logging, and persistence.
sidebar_position: 2
---

`ProcedoHostBuilder` is the main high-level composition entry point for embedding Procedo.

## Current Builder Surface

The current builder includes methods such as:

- `Configure(...)`
- `ConfigurePlugins(...)`
- `UseServiceProvider(...)`
- `ConfigureExecution(...)`
- `ConfigureValidation(...)`
- `UseLogger(...)`
- `UseEventSink(...)`
- `UseRunStateStore(...)`
- `UseLocalRunStateStore(...)`
- `UseWorkflowDefinitionResolver(...)`
- `Build()`

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.Extensible
```

## Why It Matters

This builder is the easiest way to centralize:

- plugin registration
- execution policy defaults
- validation behavior
- logger selection
- event sink selection
- persistence configuration
- workflow definition resolution for callback-driven resume

## Related Content

- [Embedding Procedo](./embedding-procedo.md)
- [Callback-Driven Resume](./callback-driven-resume.md)
- [Custom Runtime Composition](./custom-runtime-composition.md)
