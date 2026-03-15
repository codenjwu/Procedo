---
title: Create Your First Workflow
description: Learn the minimum YAML you need to author a Procedo workflow.
sidebar_position: 3
---

This page shows the smallest useful Procedo workflow shape and explains what each part does.

If you are new to Procedo, start here after you have successfully run the built-in hello example.

## Minimal Working Example

```yaml
name: hello_echo
version: 1

stages:
- stage: demo
  jobs:
  - job: simple
    steps:
    - step: hello
      type: system.echo
      with:
        message: "Hello Procedo"
```

## How It Works

- `name` gives the workflow a stable identifier that appears in logs and run state.
- `version` marks the workflow document version.
- `stages` defines the top-level execution phases.
- `stage` names a phase of work.
- `jobs` groups work inside the stage.
- `job` names a unit of related steps.
- `steps` contains the executable actions.
- `step` gives the action a local identifier.
- `type` selects the step implementation.
- `with` passes input values into the step.

## Why The Structure Is Nested

Procedo uses a staged structure so larger workflows stay readable as they grow.

Even though this example is tiny, it already follows the same shape used by more advanced workflows:

1. workflow
2. stage
3. job
4. step

That means the simple example teaches the same mental model you will use later for conditions, outputs, persistence, and templates.

## Save And Run The Workflow

Save the YAML to a file such as `my-first-workflow.yaml`, then run:

```powershell
dotnet run --project src/Procedo.Runtime -- my-first-workflow.yaml
```

## Expected Result

If the workflow is valid and the built-in system plugin is available, you should see:

- the workflow start
- the `demo` stage
- the `simple` job
- the `hello` step executing
- the message `Hello Procedo`
- a successful completion message

## Common First Mistakes

- omitting `version`
- placing `steps` directly under `stages` instead of under `jobs`
- forgetting the `type` field on a step
- using a step type that is not registered

## Next Steps

- [Workflow Structure Overview](../author-workflows/workflow-structure-overview.md)
- [Steps](../author-workflows/steps.md)
