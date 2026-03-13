# Persistence

Procedo supports local persistence-backed execution for single-node workflow runs.

This enables:

- run state capture
- step state capture
- resume by `runId`
- completed-step skipping on resume
- output/variable rehydration for resumed runs
- persisted waiting state for generic pause/wait/resume flows
- corruption detection for malformed or unsupported persisted run files

## Phase 1 scope

Phase 1 persistence is local and file-backed.

It is intended for:

- single-node reliability
- crash/retry recovery
- resume after transient failure
- operational visibility into local run state

It is not intended yet for:

- distributed coordination
- shared multi-node execution
- high-concurrency remote orchestration

## Main concepts

### Run state

A workflow run has a unique `runId` and persisted run-level state.

Run state includes a persistence schema version so future store changes can be handled explicitly.

### Step state

Each step can have persisted execution state such as:

- status
- outputs
- error details
- timestamps or related run metadata

### Resume

When resuming:

- completed steps should be skipped
- downstream context should be reconstructed from persisted outputs/variables
- unfinished or failed work can continue from the stored run state

## Current implementation

The current built-in local store is file-backed.

Reference type:

- `Procedo.Persistence.Stores.FileRunStateStore` from the `Procedo.Hosting` package

For most consumers, the important part is not the concrete store type but the hosting API:

- `ProcedoHostBuilder.UseLocalRunStateStore(...)`

Host usage:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs")
    .Build();
```

Resume usage:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs", resumeRunId: "run-123")
    .Build();
```

## Waiting and resume signals

Procedo now supports a generic persistence-backed waiting model for single-node hosts.

A step can return:

- `Waiting = true`
- an optional `WaitDescriptor`

When that happens, Procedo will:

- mark the step as `Waiting`
- mark the run as `Waiting`
- persist the waiting metadata and run state
- stop scheduling further work

To continue the workflow, call the resume path with the same `runId` and a `ResumeRequest` payload.

Current semantics for this first version are intentionally simple:

- one waiting step pauses the whole run
- resume re-executes the waiting step
- the resumed step can inspect `StepContext.Resume`
- downstream work continues only after that step completes successfully
- persisted waiting runs can be enumerated by the local runtime host for operator inspection
- persisted run-state files can be inspected and deleted by the local runtime host for operator cleanup

This model is generic and is not limited to approval scenarios. It can support:

- operator confirmation
- external callback signals
- file-arrival or polling coordination
- host-managed checkpoints or deferred continuation

## Reliability behavior

`FileRunStateStore` now uses a temp-file plus replace/move write path so saves do not rewrite the destination file in-place.

It also provides:

- current schema stamping on save
- backward-compatible loading of legacy files with no schema marker
- rejection of unsupported future schema versions
- clear `InvalidDataException` failures for malformed persisted JSON

## Operational recommendations

For Phase 1 production use on a single machine:

- store run state under a dedicated application directory
- avoid sharing the same state directory across unrelated applications
- back up or retain run state according to operational needs
- monitor disk usage if persistence is enabled heavily
- document cleanup/retention behavior in the embedding application
- treat `runId` values as file-safe identifiers

## Reliability expectations

Persistence should support:

- local workflow resume after expected failures
- reproducible local debugging
- deterministic skipping of already-completed work
- explicit failure when persisted state is corrupted or too new for the current binary

Remaining hardening work for Phase 1 includes:

- richer recovery tooling for damaged state files
- waiting-safe retention/cleanup refinements
- optional backup/rotation policy for persisted run state

## Current limitations

At the current stage, users should be aware of these limitations:

- persistence is local, not distributed
- corrupted state currently fails fast instead of self-healing
- long-term schema migration rules are still intentionally conservative
- external cleanup/retention policies are host/application concerns for now

## Recommended documentation/usage policy

For Phase 1, describe persistence as:

- local persistence-backed execution
- resume support for single-node hosts
- schema-aware and corruption-aware local state handling

## Related references

Useful references:

- [Embedding Procedo](/D:/Project/codenjwu/Procedo/docs/EMBEDDING_PROCEDO.md)
- [Runtime Runbook](/D:/Project/codenjwu/Procedo/docs/RUNBOOK.md)
- [Phase 1 Release Checklist](/D:/Project/codenjwu/Procedo/docs/PHASE1_RELEASE_CHECKLIST.md)
- `examples/Procedo.Example.PersistenceResume`

## Inspecting a persisted run

```powershell
dotnet run --project src/Procedo.Runtime -- --show-run <runId> --state-dir .procedo/runs
```

The runtime summary includes run status, timestamps, source path when available, waiting details, step counts, and a per-step status list.

## Bulk cleanup of persisted runs

```powershell
dotnet run --project src/Procedo.Runtime -- --delete-completed --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --delete-failed --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --delete-all-older-than 1.00:00:00 --state-dir .procedo/runs
dotnet run --project src/Procedo.Runtime -- --delete-waiting-older-than 02:00:00 --state-dir .procedo/runs
```

Only one bulk delete filter can be used at a time. `--delete-waiting-older-than` uses `WaitingSinceUtc` when available and falls back to the run update timestamp otherwise.

