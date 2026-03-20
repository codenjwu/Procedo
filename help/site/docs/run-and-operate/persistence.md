---
title: Persistence
description: Persist workflow run state so waiting or interrupted work can continue later.
sidebar_position: 1
---

Persistence lets Procedo store run state outside the active process.

This is important for workflows that:

- wait for human approval
- need to resume later
- should leave behind inspectable run state
- need operational recovery beyond a single process lifetime
- need host-driven wait queries and callback-style resume

## Minimal Working Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/16_persistence_resume_happy_path.yaml --persist --state-dir .procedo/runs
```

The current repository was validated with the same command shape using a temporary state directory.

## Why Use Persistence

- resume workflows later
- inspect saved state
- support wait-and-continue patterns
- support callback-driven resume by wait identity

## What To Expect

On a successful persisted run, the runtime prints the run id and the state directory used to store the run record.

For example:

```text
[INFO] Workflow 'persistence_resume_happy_path' completed successfully.
[INFO] Run id: <runId>
[INFO] Run state directory: <state directory path>
```

If a workflow enters a waiting state, the persisted run keeps:

- run and step status
- wait identity metadata
- workflow snapshot information for safe resume
- enough state for later inspection or cleanup

## Resume Models

Procedo supports two persisted resume patterns:

1. resume by `runId`
2. resume by wait identity from a host application

The CLI focuses on the first model.

The embedding host APIs support the second model through active-wait queries and `ResumeWaitingRunAsync(...)`.

## Local File-Backed Scope

The built-in persistence model is designed for single-node local execution.

It includes:

- atomic file replacement writes
- persisted workflow snapshots for callback-driven resume safety
- local-process and local-machine concurrency protection for the built-in file store

It is not a distributed orchestration store.

## When To Reach For It

Use persistence early if your workflows are meant to act like operational processes rather than short local scripts.

It becomes especially important once you introduce:

- wait steps
- resume signals
- approval checkpoints
- external inspection of run state

## Related Content

- [Observability](./observability.md)
- [CLI Overview](../reference/cli-overview.md)
- [Callback-Driven Resume](../use-in-dotnet/callback-driven-resume.md)
