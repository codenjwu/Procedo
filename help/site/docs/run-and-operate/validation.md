---
title: Validation
description: Catch workflow errors early before execution reaches runtime.
sidebar_position: 3
---

Validation helps you detect structural, dependency, and parameter issues before a workflow runs.

## Why Validation Matters

Validation is one of Procedo's most practical features. It keeps obvious workflow problems from turning into confusing runtime failures.

Typical issues caught by validation include:

- invalid step types
- unknown dependencies
- dependency cycles
- invalid parameter values

## Example Sources

- `examples/13_missing_plugin_validation_error.yaml`
- `examples/14_cycle_dependency_validation_error.yaml`
- `examples/49_parameter_schema_validation_demo.yaml`

## Valid Input Example

This command was validated successfully in the current repository:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

## Real Validation Failure Examples

Running the invalid examples currently produces errors like:

```text
[ERROR] PV304 ... No plugin registered for step type 'no.such.plugin'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

and:

```text
[ERROR] PV309 ... Cyclic dependency detected in stage 'validate', job 'cycle'.
[ERROR] [PR201] Workflow validation failed. Fix validation errors before execution.
```

## Why It Matters

- fail fast on invalid workflows
- catch dependency issues early
- verify expected runtime input shape

## Good Practice

Treat validation as part of normal authoring, not just something you do when a workflow is already broken.

## Related Content

- [Common Validation Errors](../troubleshooting/common-validation-errors.md)
- [Parameters](../author-workflows/parameters.md)
