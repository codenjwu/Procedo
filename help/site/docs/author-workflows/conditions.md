---
title: Conditions
description: Gate step execution at runtime with Procedo condition expressions.
sidebar_position: 5
---

Use `condition:` to decide whether a step should run.

Conditions are evaluated at runtime. They are useful when the workflow shape stays the same, but certain steps should only execute under specific input or state.

## Minimal Working Example

```yaml
steps:
- step: announce
  type: system.echo
  condition: eq(params.environment, 'prod')
  with:
      message: "Deploying to production"
```

## Try A Working Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=dev
```

This command has been validated in the current repository.

## What Happens In The Example

The example workflow:

- always announces the release label
- skips the production deployment step when `environment` is not `prod`
- runs the non-production deployment path instead
- runs an additional validation step only for the development case

The important runtime lines are:

```text
Release orders-api-dev
Dry-run deployment for orders-api-dev
Skipping [deploy/main/deploy_prod] because condition 'eq(params.environment, 'prod')' evaluated to false.
Validated development release orders-api-dev
```

## When To Use Conditions

- environment-specific deployment paths
- optional verification steps
- guarded production-only actions
- conditional cleanup or packaging

## Conditions Versus Templates

Use `condition:` when the step still exists in the workflow, but should sometimes be skipped at runtime.

Use templates when you want to change the generated workflow structure before runtime.

## Related Content

- [Conditional Execution](../recipes/conditional-execution.md)
- [YAML Workflow Schema Overview](../reference/yaml-workflow-schema-overview.md)
