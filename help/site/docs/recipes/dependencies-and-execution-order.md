---
title: Dependencies and Execution Order
description: Use depends_on to control step ordering and represent simple DAG patterns.
sidebar_position: 4
---

Use `depends_on` when execution order matters.

This is how you express that one step must wait for another step to complete before it can run.

## Linear Dependency Example

The repository includes a simple validated chain:

```yaml
name: linear_depends_on
version: 1
stages:
- stage: build
  jobs:
  - job: pipeline
    steps:
    - step: download
      type: system.echo
      with: { message: "download" }
    - step: parse
      type: system.echo
      depends_on:
      - download
      with: { message: "parse" }
    - step: save
      type: system.echo
      depends_on:
      - parse
      with: { message: "save" }
```

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/02_linear_depends_on.yaml
```

## Fan-Out And Fan-In Example

The repository also includes a validated branching example:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/03_fan_out_fan_in.yaml
```

That workflow shows:

- one seed step
- two parallel branches that both depend on the seed
- one merge step that waits for both branches

## When To Use Dependencies

- a step consumes outputs from another step
- work must happen in a strict order
- you want a branch-and-merge execution pattern
- a final packaging or summary step should wait for multiple prerequisites

## Related Content

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Passing Data Between Steps](./passing-data-between-steps.md)
- [Conditions](../author-workflows/conditions.md)
