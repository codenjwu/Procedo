---
title: Minimal Pipeline
description: Start from the smallest Procedo workflow you can run successfully.
sidebar_position: 1
---

This recipe gives you the smallest useful Procedo workflow.

It is the best copy-and-modify starting point for a brand new workflow file.

## Workflow

```yaml
name: hello_echo
version: 1

stages:
- stage: main
  jobs:
  - job: hello
    steps:
    - step: say_hello
      type: system.echo
      with:
        message: "Hello from Procedo"
```

## Run It

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

## Why This Is A Good Starting Point

- it has the full required workflow shape
- it is small enough to understand in one read
- it already uses a real built-in step type
- it gives you a safe base for experimenting with new fields

## Good First Modifications

After you run it successfully, try one change at a time:

- rename the workflow
- change the message
- add a second step
- add a parameter and reference it in the message

## Related Content

- [Run Your First Workflow](../get-started/run-your-first-workflow.md)
- [Create Your First Workflow](../get-started/create-your-first-workflow.md)
