---
title: Procedo CLI Basics
description: Learn the main Procedo CLI patterns for running, resuming, and inspecting workflows.
sidebar_position: 4
---

The Procedo CLI host is the fastest way to run examples and inspect runtime behavior.

The command shape is:

```powershell
dotnet run --project src/Procedo.Runtime -- [workflow.yaml] [options]
```

## Common Tasks

Run a workflow:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

Run with parameters:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/49_parameter_schema_validation_demo.yaml --param service_name=orders-api --param environment=prod --param retry_count=3
```

Run with persisted state:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/runs
```

Inspect CLI help:

```powershell
dotnet run --project src/Procedo.Runtime -- --help
```

## Core Options

The current runtime help lists these core capabilities:

- `--param <key=value>` to pass runtime parameter values
- `--persist` to write run state to local storage
- `--resume <runId>` to continue a persisted run
- `--resume-signal <type>` to provide a resume signal for waiting workflows
- `--state-dir <path>` to control where run state is stored
- `--list-waiting` to inspect runs currently in a waiting state
- `--show-run <runId>` to inspect a persisted run
- `--delete-run <runId>` to remove stored run state
- `--events-console` to emit structured events to console
- `--events-json <path>` to emit structured events to a JSONL file

## When To Reach For The CLI

Use the CLI when you want to:

- run a workflow file directly
- test workflow authoring changes quickly
- validate parameter behavior
- experiment with persistence and resume
- inspect runtime state without writing an embedding app

## Related Content

- [CLI Overview](../reference/cli-overview.md)
- [Persistence](../run-and-operate/persistence.md)
