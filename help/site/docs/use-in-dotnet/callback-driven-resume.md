---
title: Callback-Driven Resume
description: Query active waits and resume persisted runs by wait identity from your .NET host.
sidebar_position: 4
---

Callback-driven resume is the host-side pattern for resuming a waiting run without already knowing its `runId`.

This is useful when your application receives an approval, callback, or other out-of-band signal and needs to match it to the correct waiting workflow.

## What You Need

Use a host with:

- a configured run-state store
- a workflow definition resolver
- the plugins required by the target workflow

For local file-backed persistence, `UseLocalRunStateStore(...)` configures both the built-in file store and the default file-based workflow resolver.

## Core Host APIs

The main host APIs are:

- `FindWaitingRunsAsync(...)`
- `ResumeWaitingRunAsync(...)`

The corresponding public models are:

- `WaitingRunQuery`
- `ActiveWaitState`
- `ResumeWaitingRunRequest`
- `WaitingRunMatchBehavior`

## Minimal Example

```csharp
using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs")
    .Build();

var waiting = await host.FindWaitingRunsAsync(new WaitingRunQuery
{
    WaitType = "signal",
    WaitKey = "callback-identity-demo",
    ExpectedSignalType = "approve"
});

var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
{
    WaitType = "signal",
    WaitKey = "callback-identity-demo",
    ExpectedSignalType = "approve",
    SignalType = "approve",
    Payload = new Dictionary<string, object>
    {
        ["approved_by"] = "ops-bot",
        ["ticket"] = "CHG-710"
    }
});
```

## Match Behavior

`ResumeWaitingRunAsync(...)` is explicit about duplicate matches.

Use `WaitingRunMatchBehavior` when more than one run could match:

- `FailWhenMultiple`
- `ResumeNewest`
- `ResumeOldest`

The safest default is `FailWhenMultiple`.

## What Active Wait Query Returns

`ActiveWaitState` gives you a host-friendly projection of the current wait:

- workflow name
- run id
- stage, job, and step identifiers
- wait type and wait key
- expected signal type
- waiting timestamp
- optional wait metadata

That lets your host application make routing decisions without reading raw persisted run-state JSON.

## Workflow Snapshot Safety

For callback-driven resume, Procedo persists a workflow snapshot with the waiting run.

That matters because the resumed run should continue with the workflow definition that entered the waiting state, not silently reload a changed YAML file later.

If a waiting run predates persisted snapshots, automatic callback-driven resume is intentionally more limited.

## Current Scope

Phase 1 callback-driven resume is production-ready for the single-node file-backed persistence model.

It is not a distributed lock or multi-node coordination feature.

## Validated Example Projects

These example projects are the best place to see the pattern in real code:

```powershell
dotnet run --project examples/Procedo.Example.CallbackResumeHost
dotnet run --project examples/Procedo.Example.CustomResolverStore
```

## Related Content

- [Embedding Procedo](./embedding-procedo)
- [ProcedoHostBuilder](./procedo-host-builder)
- [Persistence](../run-and-operate/persistence)
- [Built-in Steps: Wait and Resume](../reference/built-in-steps-wait-and-resume)
