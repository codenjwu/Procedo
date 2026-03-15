---
id: runtime-error-codes
title: Runtime Error Codes
sidebar_label: Error Codes
description: Reference for Procedo runtime error codes, what they mean, and how to interpret them during validation, execution, waiting, and resume flows.
---

# Runtime error codes

Procedo uses short `PRxxx` error codes for common runtime and host-level outcomes. These constants are defined in `src/Procedo.Core/Models/RuntimeErrorCodes.cs`.

Use these codes as the durable identifier for error handling, dashboards, and troubleshooting. Error messages can change in wording over time, but the codes are a better fit for automation.

## Error code table

| Code | Name | Meaning |
| --- | --- | --- |
| `PR000` | `None` | No runtime error was recorded |
| `PR100` | `JobFailed` | A job failed because one of its steps or dependencies did not complete successfully |
| `PR101` | `PluginNotFound` | A referenced step type could not be resolved from the registered plugins |
| `PR102` | `StepResultFailed` | A step returned a failed result explicitly |
| `PR103` | `StepException` | A step threw an exception during execution |
| `PR104` | `StepTimeout` | A step exceeded its allowed runtime or timeout policy |
| `PR105` | `Cancelled` | The run was cancelled before successful completion |
| `PR106` | `DependencyBlocked` | A step or job could not run because an upstream dependency failed or was blocked |
| `PR107` | `SchedulerDeadlock` | The engine detected a graph progress problem and could not continue scheduling |
| `PR108` | `Waiting` | The run is paused in a waiting state rather than finished |
| `PR109` | `InvalidResume` | A resume request was invalid for the current persisted run state |
| `PR200` | `WorkflowLoadFailed` | The workflow file or source could not be loaded correctly |
| `PR201` | `ValidationFailed` | Validation failed before execution could begin |
| `PR202` | `ConfigurationInvalid` | Runtime configuration was invalid |
| `PR203` | `WorkflowFileNotFound` | The supplied workflow file path did not exist |

## Most common codes in practice

These are usually the first ones operators encounter:

- `PR201`: invalid YAML shape, dependency errors, unknown step types, or invalid parameter input
- `PR101`: plugin registration mismatch
- `PR108`: waiting workflows such as `system.wait_signal`
- `PR109`: trying to resume the wrong run or using resume data that does not match the waiting state

## Validation vs execution

It helps to separate codes into two groups:

Validation and setup:

- `PR200`
- `PR201`
- `PR202`
- `PR203`

Execution and runtime:

- `PR100`
- `PR101`
- `PR102`
- `PR103`
- `PR104`
- `PR105`
- `PR106`
- `PR107`
- `PR108`
- `PR109`

## Waiting is not the same as failure

`PR108` means the workflow intentionally paused. In the CLI host, that state is also reflected by exit code `2`, which allows automation to distinguish:

- success
- failure
- paused and resumable

That behavior is validated by the docs snippet suite with `examples/45_wait_signal_demo.yaml`.

## Troubleshooting guidance

When you surface errors in your own tools:

- show both the code and the message
- link the code back to user-facing troubleshooting docs
- include step id, source path, and wait metadata when available

## Related content

- [Common Validation Errors](../troubleshooting/common-validation-errors)
- [Runtime Statuses](./runtime-statuses)
- [CLI Overview](./cli-overview)
- [Validation](../run-and-operate/validation)
