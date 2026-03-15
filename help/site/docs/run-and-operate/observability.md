---
title: Observability
description: Inspect Procedo execution through console and structured event output.
sidebar_position: 2
---

Procedo can emit structured execution events so you can understand what happened during a run.

Observability matters once workflows stop being trivial. You need to know:

- what started
- what ran
- what was skipped
- what failed
- what completed
- what should be inspected later

## Minimal Working Example

```powershell
dotnet run --project src/Procedo.Runtime -- examples/18_observability_console_events.yaml --events-console
```

Write JSONL events:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/19_observability_jsonl_events.yaml --events-json .procedo/events.jsonl
```

## When To Use Each Option

- `--events-console` is best for local development and quick inspection
- `--events-json` is better when you want a structured event trail you can retain or process later

## What This Gives You

Structured observability helps you:

- debug execution order
- review workflow behavior after a run
- preserve an event history for troubleshooting
- prepare for future sinks or external analysis

## Related Content

- [Persistence](./persistence.md)
- [CLI Overview](../reference/cli-overview.md)
