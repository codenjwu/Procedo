---
id: runtime-persistence-state
title: Runtime Persistence State
sidebar_label: Persistence State
description: Learn what Procedo stores for persisted runs and how run status, step status, outputs, and wait metadata are represented.
---

# Runtime persistence state

When you run Procedo with persistence enabled, the runtime writes a serialized run model to disk. That model is the source of truth for resuming waiting workflows, inspecting past runs, and understanding what the engine considers already completed.

The main persisted types live in:

- `src/Procedo.Core/Runtime/WorkflowRunState.cs`
- `src/Procedo.Core/Runtime/StepRunState.cs`
- `src/Procedo.Core/Runtime/WaitDescriptor.cs`

## Workflow-level fields

The persisted `WorkflowRunState` includes these key fields:

| Field | Meaning |
| --- | --- |
| `PersistenceSchemaVersion` | Version marker for the persisted file shape |
| `RunId` | Stable identifier used for inspection and resume |
| `WorkflowName` | The workflow `name` from YAML |
| `WorkflowVersion` | The workflow `version` from YAML |
| `Status` | Current run status such as `Running`, `Waiting`, or `Completed` |
| `Error` | Top-level error text when the run fails |
| `CreatedAtUtc` | When the run record was created |
| `UpdatedAtUtc` | When the run record last changed |
| `WaitingStepKey` | Step identifier of the waiting step, if the run is paused |
| `WaitingSinceUtc` | Timestamp for when the run entered waiting state |
| `Steps` | Per-step runtime state keyed by step identifier |

## Step-level fields

Each step entry in `WorkflowRunState.Steps` is a `StepRunState` with:

| Field | Meaning |
| --- | --- |
| `Stage` | Owning stage name |
| `Job` | Owning job name |
| `StepId` | The step identifier from YAML |
| `Status` | Current step status |
| `Error` | Step-specific failure text |
| `StartedAtUtc` | Start timestamp when execution began |
| `CompletedAtUtc` | Completion timestamp when execution finished |
| `Outputs` | Output values preserved for downstream expression resolution and resume |
| `Wait` | `WaitDescriptor` data when the step pauses the workflow |

## Wait descriptor fields

When a step pauses execution, Procedo persists a `WaitDescriptor`:

| Field | Meaning |
| --- | --- |
| `Type` | Wait mechanism such as signal, file, or time-based waiting |
| `Reason` | Human-readable explanation when available |
| `Key` | Resume correlation key if the wait type uses one |
| `Metadata` | Extra details needed by the runtime or operator |

That model is what makes local resume possible without re-running already completed steps.

## What persistence is for

Persistence is designed for:

- local host recovery
- operator inspection
- pause/resume workflows
- output rehydration for downstream steps after resume

It is not a clustered, distributed orchestration state store. The current support model is still single-node and file-backed.

## Example operator flow

Start a persisted run:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/help-docs-runs
```

Pause in a waiting state:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/help-wait-runs
```

In both cases, Procedo stores enough information to identify the run, inspect step outcomes, and continue later from persisted state.

## What embedders should assume

If you build tooling around persisted state:

- treat the runtime model as operational data, not a public long-term storage contract
- rely on documented status meanings and error codes, not incidental field ordering or file formatting
- expect `Outputs` to be the canonical source for resume-time downstream expression evaluation

## Related content

- [Persistence](../run-and-operate/persistence)
- [Runtime Statuses](./runtime-statuses)
- [Built-in Steps: Wait and Resume](./built-in-steps-wait-and-resume)
- [Known Limitations](../whats-new/known-limitations)
