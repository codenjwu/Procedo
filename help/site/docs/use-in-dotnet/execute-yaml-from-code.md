---
title: Execute YAML from Code
description: Load a workflow file or YAML string and execute it directly from your .NET application.
sidebar_position: 3
---

One of the simplest embedding patterns is loading YAML in-process and executing it directly.

## Low-Level Parse, Validate, Execute Flow

The validated basic example uses this shape:

```csharp
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
var workflow = new YamlWorkflowParser().Parse(yaml);

IPluginRegistry registry = new PluginRegistry();
registry.AddSystemPlugin();
registry.AddDemoPlugin();

var validation = new ProcedoWorkflowValidator().Validate(workflow, registry);
var engine = new ProcedoWorkflowEngine();
var result = await engine.ExecuteAsync(workflow, registry, new ConsoleLogger()).ConfigureAwait(false);
```

## Validated Example

```powershell
dotnet run --project examples/Procedo.Example.Basic
```

## When To Use This Pattern

- explicit control over parse/validate/execute phases
- custom pre-processing around workflow loading
- apps that want lower-level runtime control

## Related Content

- [Embedding Procedo](./embedding-procedo.md)
- [ProcedoHostBuilder](./procedo-host-builder.md)
