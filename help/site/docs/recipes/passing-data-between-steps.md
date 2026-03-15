---
title: Passing Data Between Steps
description: Use outputs and expressions to move data through a Procedo workflow.
sidebar_position: 2
---

Use step outputs when later work depends on values produced earlier in the workflow.

This is one of the most useful basic recipes because it turns a list of isolated steps into a real data flow.

## Try The Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/05_outputs_and_expressions.yaml
```

The command has been validated successfully in the current repository.

## What To Look For

- a step produces a value
- a later step consumes that value
- expression binding connects them

## Expected Result

The key output is:

```text
alpha
from producer: alpha
```

That second line confirms the later step consumed output from the earlier one.

## Related Content

- [Outputs](../author-workflows/outputs.md)
- [Conditions](../author-workflows/conditions.md)
