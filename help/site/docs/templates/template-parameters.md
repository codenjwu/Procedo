---
title: Template Parameters
description: Define parameterized template inputs and override them from child workflows or runtime callers.
sidebar_position: 2
---

Template parameters define the values a base template expects from its consumer or from the runtime.

## Common Pattern

Base template:

```yaml
parameters:
  service_name:
    type: string
    required: true
  environment:
    type: string
    default: dev
  region:
    type: string
    default: eastus
```

Child workflow:

```yaml
parameters:
  service_name: procedo
```

Runtime override:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## What The Current Model Allows

The current template model allows child workflows to override:

- parameter values
- workflow variables
- top-level execution settings like `max_parallelism` and `continue_on_error`
- workflow `name`

## What Child Workflows Cannot Redefine

In the current implementation, child workflows may not define:

- new parameter schema definitions
- new stages
- new jobs
- new steps

## When To Use Template Parameters

- environment selection
- service or application naming
- region and rollout targeting
- structured metadata passed into a reusable flow

## Related Content

- [Templates Overview](./templates-overview.md)
- [Template Limitations](./template-limitations.md)
