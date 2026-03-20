# Callback-Driven Resume Requirements

## Purpose

This document defines the generic Procedo enhancements required to support callback-driven workflow resume scenarios for host applications such as ProtoScope.

The goal is to let an external orchestrator:

1. start a workflow,
2. let the workflow enter a waiting state,
3. later receive an external event or callback,
4. locate the matching waiting run using generic wait identity data,
5. resume that run safely,
6. continue this pattern across repeated wait and resume cycles.

Procedo should provide the generic wait and resume primitives. Host-specific protocol behavior should remain outside Procedo.

## Problem Statement

Procedo already supports:

- waiting steps through `WaitDescriptor`,
- persisted waiting state through `WorkflowRunState.WaitingStepKey`,
- manual resume by `RunId`,
- signal-based resume via `ResumeRequest`.

What is missing is a first-class, generic way to:

- find waiting runs by wait identity instead of scanning all runs,
- resume a waiting run by wait criteria instead of raw run id,
- handle duplicate matches deterministically,
- do this safely under concurrency,
- expose active waiting state through a stable query model.

Without these capabilities, host applications must inspect persistence internals and implement their own waiting-run search logic, which is brittle and leaks engine details.

## Scope

Implement generic engine-level support for:

- querying active waiting runs,
- identifying waiting runs by wait type, wait key, and metadata,
- resuming a waiting run by wait identity,
- deterministic behavior when multiple waiting runs match,
- concurrency-safe resume semantics,
- tests covering repeated wait and resume cycles.

## Non-Goals

Do not implement any of the following in Procedo:

- HTTP callback handling,
- webhook routing,
- WebSocket listeners,
- protocol payload parsing,
- session or message models,
- product-specific correlation rules,
- ProtoScope stream events,
- UI workflows.

Those concerns belong in the host application.

## Design Principles

- Keep the API generic and host-agnostic.
- Keep persistence abstractions clean and queryable.
- Do not force callers to enumerate every run to find waiting matches.
- Make duplicate-match behavior explicit.
- Make stale or already-resumed waits fail safely.
- Preserve compatibility with the existing `ResumeAsync(..., runId, ResumeRequest, ...)` path.

## Required Enhancements

### 1. Add a stable active-wait query model

Introduce a lightweight public model that represents the active waiting state of a run without requiring callers to understand `WorkflowRunState` internals.

Suggested shape:

```csharp
public sealed class ActiveWaitState
{
    public string RunId { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public RunStatus RunStatus { get; set; }
    public DateTimeOffset? WaitingSinceUtc { get; set; }

    public string Stage { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;
    public string StepId { get; set; } = string.Empty;
    public string StepPath { get; set; } = string.Empty;

    public string WaitType { get; set; } = string.Empty;
    public string? WaitKey { get; set; }
    public string? WaitReason { get; set; }
    public IDictionary<string, object> Metadata { get; set; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
```

This type may be named differently, but it should expose the same information.

### 2. Add a waiting-run query API

Expose a generic way to query active waiting runs without requiring host applications to call `ListRunsAsync()` and filter in memory.

Suggested request model:

```csharp
public sealed class WaitingRunQuery
{
    public string? WorkflowName { get; set; }
    public string? WaitType { get; set; }
    public string? WaitKey { get; set; }
    public string? StepId { get; set; }
    public string? ExpectedSignalType { get; set; }
    public bool IncludeMetadata { get; set; } = true;
    public int? Limit { get; set; }
}
```

Suggested abstraction additions:

```csharp
Task<IReadOnlyList<ActiveWaitState>> FindWaitingRunsAsync(
    WaitingRunQuery query,
    CancellationToken cancellationToken = default);
```

This can live either:

- on `IRunStateStore`, or
- behind a new dedicated query abstraction,

but it must be a first-class public capability.

### 3. Add resume-by-wait-identity support

Expose an engine API that resumes a waiting run using wait identity criteria instead of requiring the caller to know the raw run id in advance.

Suggested request model:

```csharp
public sealed class ResumeWaitingRunRequest
{
    public string? WorkflowName { get; set; }
    public string WaitType { get; set; } = string.Empty;
    public string? WaitKey { get; set; }
    public string? StepId { get; set; }
    public string? ExpectedSignalType { get; set; }

    public string SignalType { get; set; } = string.Empty;
    public IDictionary<string, object> Payload { get; set; } =
        new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public WaitingRunMatchBehavior MatchBehavior { get; set; } =
        WaitingRunMatchBehavior.FailWhenMultiple;
}
```

Suggested enum:

```csharp
public enum WaitingRunMatchBehavior
{
    FailWhenMultiple = 0,
    ResumeNewest = 1,
    ResumeOldest = 2
}
```

Suggested engine API:

```csharp
Task<WorkflowRunResult> ResumeWaitingRunAsync(
    WorkflowDefinition workflow,
    IPluginRegistry pluginRegistry,
    ILogger logger,
    IRunStateStore runStateStore,
    ResumeWaitingRunRequest request,
    IExecutionEventSink? executionEventSink = null,
    CancellationToken cancellationToken = default,
    WorkflowExecutionOptions? executionOptions = null);
```

The exact signature may differ, but the capability should exist.

### 4. Add deterministic duplicate-match handling

If multiple waiting runs match the same wait criteria, Procedo must not silently choose one by accident.

Required behavior:

- default behavior must be failure,
- the exception should clearly describe the ambiguity,
- caller-selected alternative behaviors may be supported, such as:
  - resume newest,
  - resume oldest.

### 5. Add concurrency-safe resume validation

Procedo should prevent double-resume of the same wait state.

Required behavior:

- if two callers race to resume the same waiting run, at most one should succeed,
- resume operations must re-check that the target run is still waiting immediately before transition,
- if the waiting state has already changed, return a clear failure,
- stale waiting matches should not produce silent success.

This may require:

- compare-and-save semantics in the persistence layer, or
- an equivalent store-level protection mechanism.

### 6. Expose expected signal type cleanly

`WaitSignalStep` currently stores `expected_signal_type` in wait metadata.

Procedo should ensure this remains easy to query and use through the new query model so a host application can:

- locate a waiting run by key,
- inspect the expected signal type,
- resume with the correct signal type,
- reject invalid signal types cleanly.

### 7. Preserve repeated wait/resume support

A workflow may:

1. run,
2. wait,
3. resume,
4. continue,
5. wait again,
6. resume again.

Procedo should support this reliably across multiple cycles in the same run.

This includes:

- updating `WaitingStepKey`,
- updating `WaitingSinceUtc`,
- clearing stale wait state when resumed,
- replacing it correctly when the workflow enters a new waiting step later.

## Suggested Persistence Work

Depending on current implementation constraints, Procedo may need store-level enhancements such as:

- querying waiting runs directly,
- filtering waiting runs by wait key/type,
- optimistic concurrency or equivalent guard during resume,
- persistence tests around duplicate and stale wait transitions.

The public design should avoid forcing every persistence implementation to expose engine internals directly.

## Acceptance Criteria

Procedo should be considered complete for this feature when all of the following are true:

- host applications can query active waiting runs using generic wait criteria,
- host applications can resume a waiting run by wait identity instead of raw run id,
- duplicate matches are handled deterministically,
- stale matches fail safely,
- repeated wait and resume cycles are supported in the same run,
- expected signal type is queryable through a clean API,
- the new APIs are covered by automated tests,
- no HTTP, webhook, WebSocket, or host-specific logic is added to Procedo.

## Required Test Scenarios

Add tests for at least these cases:

1. workflow enters waiting state and can be found by wait type and key,
2. workflow resumes successfully by wait identity,
3. no match returns a clear failure,
4. multiple matches fail by default,
5. multiple matches can resume newest when explicitly requested,
6. signal type mismatch fails clearly,
7. stale match fails after another caller has already resumed the run,
8. the same run can wait, resume, wait again, and resume again,
9. waiting-run query returns the correct stage/job/step metadata,
10. expected signal type is visible in the query result.

## Recommended Implementation Order

1. Add query models for active waiting state.
2. Add wait-state query support in persistence and abstractions.
3. Add engine-level resume-by-wait-identity support.
4. Add duplicate-match and concurrency handling.
5. Add tests for repeated wait and resume cycles.
6. Document the new APIs in Procedo docs.

## Integration Boundary for Host Applications

After this work exists, a host application should be able to:

1. receive an external event,
2. derive `wait type`, `wait key`, and `signal type`,
3. call Procedo’s generic waiting-run resume API,
4. pass the external payload as resume input,
5. avoid inspecting raw persisted run objects directly.

That is the intended boundary.

## Backlog tracking

Implementation follow-up and hardening work is tracked separately in:

- [CALLBACK_DRIVEN_RESUME_BACKLOG.md](/D:/Project/codenjwu/Procedo/docs/CALLBACK_DRIVEN_RESUME_BACKLOG.md)
