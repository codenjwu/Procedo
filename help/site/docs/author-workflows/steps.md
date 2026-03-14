---
title: Steps
description: Learn how Procedo steps are defined and how step inputs are provided.
sidebar_position: 2
---

A step is the smallest executable unit in a Procedo workflow.

Every piece of actual work in Procedo happens in a step.

## Minimal Working Example

```yaml
steps:
- step: announce
  type: system.echo
  with:
    message: "Hello from a step"
```

## Key Fields

- `step` gives the step an identifier.
- `type` selects the implementation.
- `with` supplies step input values.
- `condition` can gate the step at runtime.

## What A Step Type Means

The `type` field chooses the implementation to run. In the launch examples, you will mostly see built-in `system.*` step types such as `system.echo`.

As Procedo grows, step types can also come from:

- built-in plugins
- application-registered plugins
- custom steps exposed by your host

## How Inputs Work

The `with` block is the step input payload. Each step type decides which input fields it understands.

For `system.echo`, the important input is `message`:

```yaml
with:
  message: "Hello from a step"
```

## Step Design Guidance

When you author workflows, a good step should:

- do one clear thing
- have a stable step id
- expose outputs if later steps need its results
- use `condition:` only when runtime gating is actually needed

## Related Content

- [Parameters](./parameters.md)
- [Conditions](./conditions.md)
