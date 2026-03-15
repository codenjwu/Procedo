---
title: YAML `with` and `depends_on`
description: Pass step inputs and express execution dependencies between steps.
sidebar_position: 6
---

`with` and `depends_on` are two of the most important step-level workflow fields.

## `with`

Use `with` to pass input values to a step type.

Example:

```yaml
with:
  message: "Hello Procedo"
```

The exact fields supported under `with` depend on the step type.

## `depends_on`

Use `depends_on` to express that a step must wait for one or more other steps.

Example:

```yaml
depends_on:
- producer
```

## Combined Example

```yaml
- step: consumer
  type: system.echo
  depends_on:
  - producer
  with:
    message: "from producer: ${steps.producer.outputs.message}"
```

## When To Use Them Together

This combination is common when:

- a step consumes another step's outputs
- execution must happen in a strict order
- a merge step waits for multiple prerequisites

## Related Content

- [Outputs](../author-workflows/outputs.md)
- [Dependencies and Execution Order](../recipes/dependencies-and-execution-order.md)
