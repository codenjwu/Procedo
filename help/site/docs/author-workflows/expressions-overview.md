---
title: Expressions Overview
description: Understand how Procedo resolves expressions in inputs, variables, and conditions.
sidebar_position: 6
---

Expressions are how Procedo turns workflow data into runtime values.

You will see expressions in places like:

- step inputs
- workflow variables
- runtime `condition:` clauses

## Expression Syntax

Procedo expressions are written inside `${...}`.

Examples:

```yaml
message: "release=${steps.vars.outputs.message}"
message: "${format('{0}-{1}', params.service_name, params.environment)}"
condition: eq(params.environment, 'prod')
```

## Common Sources

Current examples and runtime behavior show these common namespaces:

- `params.` for workflow parameters
- `vars.` for workflow variables
- `steps.<stepId>.outputs.<name>` for step outputs

## Supported Expression Patterns

The current examples and resolver support patterns such as:

- value substitution
- string formatting with `format(...)`
- boolean checks with `eq`, `ne`, `and`, `or`, and `not`
- collection and string helpers such as `contains`, `startsWith`, `endsWith`, and `in`

## Validated Example

The repository includes a focused runtime function showcase:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/58_runtime_expression_function_showcase.yaml
```

## How To Think About Expressions

- use expressions to connect workflow data
- keep expressions readable and purposeful
- move repeated derived values into variables when expressions start getting long

## Related Content

- [Variables](./variables.md)
- [Outputs](./outputs.md)
- [Conditions](./conditions.md)
