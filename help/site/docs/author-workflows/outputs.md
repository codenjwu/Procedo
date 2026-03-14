---
title: Outputs
description: Expose values from one step so later steps can consume them.
sidebar_position: 4
---

Outputs are how steps pass useful data forward through a workflow.

This is one of the most important workflow authoring patterns, because it lets you keep work declarative instead of collapsing everything into one large script step.

## Minimal Working Example

The repository example below has been validated successfully:

```yaml
name: outputs_and_expressions
version: 1
stages:
- stage: expressions
  jobs:
  - job: map
    steps:
    - step: producer
      type: system.echo
      with:
        message: "alpha"
    - step: consumer
      type: system.echo
      depends_on:
      - producer
      with:
        message: "from producer: ${steps.producer.outputs.message}"
```

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/05_outputs_and_expressions.yaml
```

## Expected Result

The key output is:

```text
alpha
from producer: alpha
```

The second step reads the output of the first step through the expression `${steps.producer.outputs.message}`.

## Why Outputs Matter

- They reduce duplication.
- They let later steps use earlier results.
- They help workflows stay declarative instead of script-heavy.

## What To Notice

- `producer` runs first
- `consumer` depends on `producer`
- the second message is assembled at runtime using the first step's output

This pattern becomes especially useful when a step calculates a path, id, hash, or status that later steps need.

## Related Content

- [Conditions](./conditions.md)
- [Passing Data Between Steps](../recipes/passing-data-between-steps.md)
