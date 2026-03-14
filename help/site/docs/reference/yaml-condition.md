---
title: YAML `condition`
description: Define runtime gating rules for individual Procedo steps.
sidebar_position: 7
---

Use `condition` to decide whether a step should run at runtime.

## Example

```yaml
condition: eq(params.environment, 'prod')
```

## What `condition` Does

- it is evaluated at runtime
- it must evaluate to a boolean
- if it evaluates to `false`, the step is skipped

## Common Sources Inside Conditions

- `params.<name>`
- `vars.<name>`
- `steps.<stepId>.outputs.<name>`

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=dev
```

## Related Content

- [Conditions](../author-workflows/conditions.md)
- [Expressions Condition Rules](./expressions-condition-rules.md)
