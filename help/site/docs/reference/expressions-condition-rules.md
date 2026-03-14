---
title: Expression Condition Rules
description: Understand the rules Procedo applies when evaluating runtime condition expressions.
sidebar_position: 4
---

Procedo uses expressions inside `condition` to decide whether a step should run.

## Rule 1: A Condition Must Evaluate To Boolean

The current resolver requires a condition to evaluate to a boolean result.

If it does not, evaluation fails.

## Rule 2: Conditions Are Evaluated At Runtime

Conditions are checked during execution, not during workflow file authoring.

That means conditions can depend on:

- parameters
- variables
- outputs from earlier steps

## Rule 3: False Means Skip, Not Failure

If a condition evaluates to `false`, the runtime skips the step.

This is normal behavior, not an execution error.

## Rule 4: Dependencies Still Matter

If a step references values produced earlier, use `depends_on` so the runtime order matches the expression's data needs.

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/53_runtime_condition_demo.yaml --param environment=prod
```

## Related Content

- [YAML `condition`](./yaml-condition.md)
- [Conditional Execution](../recipes/conditional-execution.md)
