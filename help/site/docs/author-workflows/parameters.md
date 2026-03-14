---
title: Parameters
description: Pass runtime inputs into Procedo workflows and validate expected values.
sidebar_position: 3
---

Parameters let a workflow accept values at runtime instead of hard-coding every decision.

They are the main way to make one workflow reusable across environments, services, and execution contexts.

## Minimal Working Example

```yaml
parameters:
  environment: prod
```

Run with overrides:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## Richer Parameter Example

Procedo also supports richer parameter definitions. The repository example below has been validated successfully with the documented command:

```yaml
parameters:
  service_name:
    type: string
    min_length: 3
    max_length: 20
    pattern: "^[a-z][a-z0-9-]+$"
    default: procedo-api

  environment:
    type: string
    allowed_values:
    - dev
    - prod
    default: dev

  retry_count:
    type: int
    min: 1
    max: 5
    default: 2
```

Run it with valid input:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## When To Use Parameters

- environment selection
- region or tenant selection
- feature toggles
- user-provided runtime values

## What Parameters Give You

- a cleaner separation between workflow shape and runtime values
- fewer duplicated workflow files
- validation at the entry point of execution
- better support for templates and promotion-style workflows

## Common Guidance

- use simple defaults when the workflow has a common path
- add constraints when invalid input would make the workflow unsafe or misleading
- prefer parameters over editing YAML per environment

## Related Content

- [Validation](../run-and-operate/validation.md)
- [YAML Workflow Schema Overview](../reference/yaml-workflow-schema-overview.md)
