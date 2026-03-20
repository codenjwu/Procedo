---
title: CLI Overview
description: Use the Procedo CLI host to run workflows, pass parameters, and inspect runtime state.
sidebar_position: 1
---

The Procedo CLI host is the main entry point for running YAML workflows directly from the repository.

The current runtime help describes the CLI as a single-node host with this usage pattern:

```text
dotnet run --project src/Procedo.Runtime -- [workflow.yaml] [options]
```

## Common Commands

Run a workflow:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/01_hello_echo.yaml
```

Resume a run by `runId`:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume <runId> --resume-signal continue --state-dir .procedo/runs
```

List waiting runs:

```powershell
dotnet run --project src/Procedo.Runtime -- --list-waiting --state-dir .procedo/runs
```

Delete an old or finished run record:

```powershell
dotnet run --project src/Procedo.Runtime -- --delete-run <runId> --state-dir .procedo/runs
```

## Core Capabilities

- run a workflow file
- provide runtime parameters
- enable persisted run state
- resume waiting workflows
- list waiting workflows
- inspect and clean stored runs
- emit structured events

The CLI is centered on persisted runs identified by `runId`.

Callback-driven resume by wait identity is a host API capability rather than a direct CLI-first flow.

## Validation Behavior

The runtime validates the workflow after loading it and before execution begins.

If validation finds errors:

- the CLI prints validation issues
- the run does not continue into execution
- the process exits with code `1`

If a workflow enters a waiting state, the CLI returns exit code `2`.

## Most Important Options

- `--param <key=value>`
- `--persist`
- `--resume <runId>`
- `--resume-signal <type>`
- `--state-dir <path>`
- `--list-waiting`
- `--show-run <runId>`
- `--delete-run <runId>`
- `--delete-waiting-older-than <timespan>`
- `--events-console`
- `--events-json <path>`

## Related Content

- [Procedo CLI Basics](../get-started/procedo-cli-basics.md)
- [Persistence](../run-and-operate/persistence.md)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume.md)
