---
title: Template Conditions
description: Use template-time branching directives to include or exclude YAML nodes before runtime.
sidebar_position: 3
---

Procedo supports template-time branching with:

- `${{ if ... }}`
- `${{ elseif ... }}`
- `${{ else }}`

These directives run before runtime step execution begins.

## Example

The repository includes a branching example:

```yaml
steps:
  ${{ if eq(params.environment, 'prod') }}:
  - step: branch_prod
    type: system.echo
    with:
      message: "Branch selected production rollout for ${vars.release_label}"
  ${{ elseif eq(params.environment, 'qa') }}:
  - step: branch_qa
    type: system.echo
    with:
      message: "Branch selected QA rollout for ${vars.release_label}"
  ${{ else }}:
  - step: branch_dev
    type: system.echo
    with:
      message: "Branch selected development rollout for ${vars.release_label}"
```

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/59_branching_operator_showcase.yaml
```

## Template-Time Versus Runtime Conditions

- use template-time directives when you want to add or remove YAML nodes
- use runtime `condition:` when a declared step should remain in the workflow but may be skipped at execution time

## Current Nuance

The current template implementation parses base-template branching against values visible while that template is expanded. Child overrides are merged later, so use runtime `condition:` when a branch must react reliably to child-supplied values.

## Related Content

- [Template Loops](./template-loops.md)
- [Conditions](../author-workflows/conditions.md)
