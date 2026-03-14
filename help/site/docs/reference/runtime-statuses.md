---
id: runtime-statuses
title: Runtime Statuses
sidebar_label: Runtime Statuses
description: Understand the run-level and step-level status values Procedo uses during execution, waiting, resume, and failure handling.
---

# Runtime statuses

Procedo tracks status at two levels:

- the overall workflow run
- each individual step inside that run

Those values are part of the persisted runtime model, the CLI output, and the structured execution events you can emit during execution. If you are operating persisted runs, investigating failures, or building your own embedding layer, these are the status values you should treat as canonical.

## Run status values

`RunStatus` is defined in `src/Procedo.Core/Runtime/RunStatus.cs`.

| Status | Meaning | Typical transition |
| --- | --- | --- |
| `Pending` | The run has been created but execution has not started yet. | Initial state before scheduling begins |
| `Running` | Procedo is actively scheduling or executing steps. | Entered after the engine begins work |
| `Waiting` | The workflow has paused for an external condition, signal, or file-based resume condition. | Returned by wait-capable steps such as `system.wait_signal` |
| `Completed` | All required work finished successfully. | Final success state |
| `Failed` | The run stopped because a step failed, a dependency was blocked, or the runtime hit a fatal execution error. | Final failure state |
| `Cancelled` | Execution was cancelled deliberately by the runtime or host. | Final cancellation state |

## Step status values

`StepRunStatus` is defined in `src/Procedo.Core/Runtime/StepRunStatus.cs`.

| Status | Meaning | Typical transition |
| --- | --- | --- |
| `Pending` | The step exists in the run graph but has not started yet. | Initial state |
| `Running` | The step is actively executing. | Entered once the engine dispatches the step |
| `Waiting` | The step yielded a wait descriptor and the run is paused. | Common with `system.wait_signal`, `system.wait_until`, and `system.wait_file` |
| `Skipped` | The step did not run because its `condition:` evaluated to false or a prior condition path made it unnecessary. | Final non-executed state |
| `Completed` | The step ran successfully and may have produced outputs. | Final success state |
| `Failed` | The step failed directly or reported a failed result. | Final failure state |

## How statuses relate to waiting workflows

When a wait-capable step pauses execution:

- the step usually moves to `Waiting`
- the workflow run moves to `Waiting`
- the persisted state captures a `WaitDescriptor`
- the CLI host returns exit code `2` to tell operators that the workflow is paused rather than failed

You can see that behavior in the validated workflow:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

## How statuses relate to skipped steps

`Skipped` is a normal outcome, not an error. It usually means:

- the step had a `condition:` that evaluated to false
- a template-expanded branch produced steps that are present in the workflow shape but not chosen at runtime

If you are inspecting a run, a skipped step should usually be explained by the workflow definition rather than treated as a runtime incident.

## Operator guidance

Use the statuses this way:

- `Completed`: safe to treat as successful run completion
- `Waiting`: the workflow needs outside action or time to continue
- `Failed`: inspect the failing step, error code, and source path
- `Skipped`: expected non-execution, usually caused by conditions

## Related content

- [Persistence](../run-and-operate/persistence)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume)
- [Runtime Persistence State](./runtime-persistence-state)
- [Runtime Error Codes](./runtime-error-codes)
