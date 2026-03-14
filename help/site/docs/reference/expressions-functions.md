---
title: Expression Functions
description: Reference the helper functions currently supported by Procedo expressions.
sidebar_position: 3
---

The current `ExpressionResolver` implementation supports a practical set of helper functions for runtime conditions and value composition.

## Common Function Groups

Boolean and comparison helpers:

- `eq(a, b)`
- `ne(a, b)`
- `and(a, b, ...)`
- `or(a, b, ...)`
- `not(a)`

String and collection helpers:

- `contains(a, b)`
- `startsWith(a, b)`
- `endsWith(a, b)`
- `in(value, list...)`

Formatting helper:

- `format(template, ...)`

## Validated Example

The repository includes a focused function showcase:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml
```

That example demonstrates:

- `or`
- `eq`
- `ne`
- `and`
- `not`
- `contains`
- `startsWith`
- `endsWith`
- `in`
- `format`

## Practical Guidance

- use `format` when you need a readable composed label
- use `in` for membership checks
- use `contains`, `startsWith`, and `endsWith` for simple string rules
- keep long nested conditions readable by extracting reusable values into variables

## Related Content

- [Expression Condition Rules](./expressions-condition-rules.md)
- [Conditions](../author-workflows/conditions.md)
