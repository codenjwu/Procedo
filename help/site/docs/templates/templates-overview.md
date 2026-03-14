---
title: Templates Overview
description: Use Procedo templates to reuse a stable workflow shape while varying parameters and variables.
sidebar_position: 1
---

Procedo templates provide a narrow, predictable reuse model.

They are a good fit when the execution graph is stable and the main differences are values such as service name, environment, region, or release metadata.

## What Templates Support

The current template model is designed for:

- reusable base workflows
- parameterized environment or deployment differences
- workflow-level variable customization
- runtime parameter overrides from the CLI or host

## Example Child Workflow

The repository includes this simple template consumer:

```yaml
template: ./templates/standard_build_template.yaml
name: template_parameters_demo

parameters:
  service_name: procedo

variables:
  artifact_name: "custom-${params.service_name}-${params.environment}"
```

## Example Base Template

The referenced base template defines the reusable workflow shape:

```yaml
name: standard_build_template
version: 1

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

variables:
  artifact_name: "${params.service_name}-${params.environment}-${params.region}"

stages:
- stage: build
  jobs:
  - job: package
    steps:
    - step: announce
      type: system.echo
      with:
        message: "Building ${vars.artifact_name}"
```

## Validated Command

```powershell
dotnet run --project src/Procedo.Runtime -- examples/48_template_parameters_demo.yaml --param environment=prod --param region=westus
```

## How To Think About Templates

- use templates when the graph stays mostly the same
- use parameters and variables to customize behavior
- use runtime `condition:` when a declared step should sometimes be skipped
- avoid treating templates like general graph composition

## Related Content

- [Template Parameters](./template-parameters.md)
- [Template Conditions](./template-conditions.md)
- [Template Limitations](./template-limitations.md)
