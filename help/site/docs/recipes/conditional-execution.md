---
title: Conditional Execution
description: Use runtime conditions to decide whether a step runs.
sidebar_position: 3
---

This recipe focuses on `condition:` and runtime gating.

## Try The Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=prod
```

The same example also works with `environment=dev`, and that variant has been validated in the current repository.

## Why This Pattern Helps

- enable environment-specific behavior
- avoid duplicating whole workflows
- keep branching explicit and reviewable

## What Changes With Parameters

- with `environment=prod`, the production path is eligible to run
- with `environment=dev`, the workflow skips the production step and follows the non-production path

This is a good example of when to use runtime conditions instead of creating separate workflow files for each environment.

## Related Content

- [Conditions](../author-workflows/conditions.md)
- [Validation](../run-and-operate/validation.md)
