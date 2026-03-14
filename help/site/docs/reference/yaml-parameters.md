---
title: YAML `parameters`
description: Define runtime inputs and constraints for Procedo workflows.
sidebar_position: 4
---

Use `parameters` to declare workflow inputs that callers can provide or override at runtime.

## Simple Shape

```yaml
parameters:
  environment: dev
```

This form is useful when you want a default value and do not need extra constraints.

## Rich Definition Shape

The current repository examples use richer parameter definitions such as:

```yaml
parameters:
  service_name:
    type: string
    min_length: 3
    max_length: 20
    pattern: "^[a-z][a-z0-9-]+$"
    default: procedo-api
```

## Supported Definition Fields

The current `ParameterDefinition` model supports:

- `type`
- `required`
- `default`
- `description`
- `allowed_values`
- `min`
- `max`
- `min_length`
- `max_length`
- `pattern`
- `item_type`
- `required_properties`

## When To Use Rich Definitions

Use richer definitions when:

- invalid input should fail fast
- users need clearer allowed values
- workflows are shared across environments or teams
- templates depend on reliable parameter shape

## Validated Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## Related Content

- [Parameters](../author-workflows/parameters.md)
- [Validation](../run-and-operate/validation.md)
