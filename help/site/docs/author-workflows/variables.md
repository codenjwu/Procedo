---
title: Variables
description: Use workflow variables to compose values once and reuse them across steps.
sidebar_position: 4
---

Variables let you name computed or repeated values so the rest of the workflow stays easier to read.

They are especially useful when multiple steps need the same derived value, such as a release label, bundle name, or service identifier.

## Minimal Working Example

The repository includes a simple validated example that produces a value in one step and reuses it in a later step:

```yaml
name: vars_expression_via_step
version: 1
stages:
- stage: vars
  jobs:
  - job: showcase
    steps:
    - step: vars
      type: system.echo
      with:
        message: "v1.2.3"
    - step: announce
      type: system.echo
      depends_on:
      - vars
      with:
        message: "release=${steps.vars.outputs.message}"
```

Run it:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/06_vars_expression_via_step.yaml
```

## Expected Result

The key lines are:

```text
v1.2.3
release=v1.2.3
```

## Variables Versus Outputs

- use workflow `variables` when you want a named value available broadly across the workflow
- use step outputs when the value is produced by a specific step at runtime

In practice, many workflows use both:

- parameters come in from the caller
- variables compose reusable values from those parameters
- outputs pass step results forward

## Good Uses For Variables

- release labels
- artifact naming
- environment-specific prefixes
- repeated message fragments
- reusable condition inputs

## Related Content

- [Parameters](./parameters.md)
- [Outputs](./outputs.md)
- [Conditions](./conditions.md)
