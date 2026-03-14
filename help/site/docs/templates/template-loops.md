---
title: Template Loops
description: Use template-time each loops to expand repeated YAML nodes from array inputs.
sidebar_position: 4
---

Procedo currently supports array-only template loops with `${{ each }}`.

## Example

```yaml
steps:
  ${{ each region in params.all_regions }}:
  - step: deploy_${region}
    type: system.echo
    condition: and(in('${region}', params.active_regions), not(contains('${region}', 'east')))
    with:
      message: "Deploy ${vars.release_label} to ${region}"
```

## What This Does

The loop expands repeated YAML nodes before runtime.

In the example above:

- one step is generated per region in `params.all_regions`
- each generated step gets a region-specific step id
- runtime `condition:` still decides whether each generated step runs

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/59_branching_operator_showcase.yaml
```

## Current Limitation

The current implementation supports array iteration only. Object or dictionary iteration is not part of this phase.

## Related Content

- [Template Conditions](./template-conditions.md)
- [Dependencies and Execution Order](../recipes/dependencies-and-execution-order.md)
