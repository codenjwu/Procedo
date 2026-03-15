---
title: Common Validation Errors
description: Understand frequent validation failures and where to start when a workflow is rejected.
sidebar_position: 1
---

Validation errors are often the fastest signal that a workflow definition is incomplete or inconsistent.

## Common Categories

- missing plugin or step implementation
- dependency cycles
- unknown dependencies
- invalid parameter input

## Real Examples

The repository includes invalid workflows that demonstrate these failures:

- `examples/13_missing_plugin_validation_error.yaml`
- `examples/14_cycle_dependency_validation_error.yaml`
- `examples/15_unknown_dependency_validation_error.yaml`

## Current Error Messages

The current runtime reports messages like:

```text
[ERROR] PV304 ... No plugin registered for step type 'no.such.plugin'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

and:

```text
[ERROR] PV309 ... Cyclic dependency detected in stage 'validate', job 'cycle'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

## How To Respond

- read the first validation error before the summary line
- fix schema or dependency issues before investigating runtime behavior
- verify the step type is registered
- check `depends_on` values for typos or cycles
- re-run the workflow after each fix

## Related Content

- [Validation](../run-and-operate/validation.md)
- [FAQ](./faq.md)
