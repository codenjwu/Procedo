---
title: Run Your First Workflow
description: Execute the smallest Procedo workflow from the CLI host.
sidebar_position: 2
---

The fastest way to understand Procedo is to run a tiny workflow end to end and inspect what the runtime prints.

This walkthrough uses the smallest repository example that succeeds with the built-in system plugin support.

## Minimal Working Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

This command has been validated in the current repository and completes successfully.

## Source Workflow

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

## What This Does

This command runs the reference CLI host against a simple YAML workflow that contains a single `system.echo` step.

The runtime:

- loads the YAML definition
- validates the workflow structure
- builds the execution graph
- runs the `system.echo` step
- prints a run summary

## Expected Output

When run in this repository, the important output is:

```text
[INFO] Starting workflow 'hello_echo'
[INFO] Stage: demo
[INFO] Job: simple
[INFO] Running [demo/simple/hello] (system.echo) attempt 1/1
Hello Procedo
[INFO] Workflow 'hello_echo' completed successfully.
```

The run id changes each time, so you should expect that line to be different on your machine.

## Why This Example Is Useful

This example is small, but it shows the full minimum shape of a Procedo workflow:

- one workflow
- one stage
- one job
- one step
- one built-in step type

Once this succeeds, you know your local environment can execute the core runtime path.

## What To Notice

- The workflow name appears in the start and completion messages.
- The runtime prints stage and job names as it enters them.
- The step id and step type appear in the execution log line.
- `system.echo` writes the configured message directly to output.

## Next Steps

- [Create Your First Workflow](./create-your-first-workflow.md)
- [Minimal Pipeline](../recipes/minimal-pipeline.md)
