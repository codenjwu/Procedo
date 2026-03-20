# Runtime Runbook

Operational runbook for the single-node Procedo runtime and reference CLI host.

## Start a run

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml
```

## Enable persistence

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --persist --state-dir .procedo/runs
```

## Pause and wait

Workflows that use `system.wait_signal`, `system.wait_until`, or `system.wait_file` may pause in a `Waiting` state.

CLI exit codes:

- `0` completed successfully
- `1` failed
- `2` waiting / paused

Common CLI/runtime codes:

- `PR101`: plugin not found for a step type
- `PR102`: step returned an unsuccessful result
- `PR103`: step threw or a runtime condition expression was invalid
- `PR104`: step timed out
- `PR107`: scheduler deadlock or unresolved dependency chain
- `PR109`: invalid resume request
- `PR200`: workflow load/template expansion failure
- `PR201`: workflow validation failure
- `PR202`: invalid CLI/config/runtime option combination
- `PR203`: workflow file not found

Example:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --persist --state-dir .procedo/runs
```

## List waiting runs

```powershell
dotnet run --project src/Procedo.Runtime -- --list-waiting --state-dir .procedo/runs
```

The command prints:

- `RunId`
- workflow name
- waiting step id
- wait type
- timestamp
- wait reason

## Resume a waiting run

Resume a waiting run with a signal:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume <runId> --resume-signal continue --state-dir .procedo/runs
```

Resume with an additional JSON payload:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume <runId> --resume-signal continue --resume-payload-json "{""approved_by"":""operator""}" --state-dir .procedo/runs
```

You can also pass a file path to `--resume-payload-json`.

For condition-based waiting steps like `system.wait_until` or `system.wait_file`, a simple signal such as `check` is enough to trigger re-evaluation.

## Host-level callback-driven resume

The reference CLI still resumes waiting runs by `runId`.

Embedding hosts now have additive APIs for callback-driven resume:

- query active waits by wait identity
- inspect wait metadata such as wait key and expected signal type
- resume a matching wait without first knowing the `runId`

This capability lives in the engine/hosting API surface rather than the CLI transport layer.

For embedding hosts, callback-driven resume now depends on:

- a store that supports conditional save semantics for concurrency-safe claims
- a persisted workflow snapshot or a custom workflow resolver that can reconstruct the original workflow definition safely

Persisted resume-by-`runId` now follows the same concurrency rule. If a store does not implement `IConditionalRunStateStore`, persisted runs can still be listed and inspected, but they cannot be resumed safely through the built-in engine resume paths.

The default file-based host path preserves the original workflow definition from the waiting run instead of silently switching to newer file contents on disk.

## Cleanup persisted run state

Delete one persisted run state file by run id:

```powershell
dotnet run --project src/Procedo.Runtime -- --delete-run <runId> --state-dir .procedo/runs
```

Use this to remove old completed, failed, or abandoned waiting runs from local development or operator-managed environments.

## Strict validation

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --strict-validation
```

## Reliability tuning

```powershell
dotnet run --project src/Procedo.Runtime -- examples/hello_pipeline.yaml --max-parallelism 4 --default-retries 2 --default-timeout-ms 5000 --continue-on-error
```

## Config precedence

Order: defaults < `procedo.runtime.json` (or `--config`) < environment variables < CLI flags.

## Common recovery

1. Capture the `runId` from runtime output.
2. Use `--list-waiting` to confirm whether the run is paused.
3. Use `--show-run <runId>` for a readable summary before opening raw state.
4. Resume with `--resume <runId> --resume-signal <type>`.
5. If the run is no longer needed, remove it with `--delete-run <runId>`.

## Operator workflow

For a typical persisted wait/resume flow:

1. Start the workflow with `--persist`.
2. If the process exits with code `2`, capture the `runId`.
3. Inspect the run with `--show-run <runId>`.
4. Check paused runs with `--list-waiting`.
5. Resume with the appropriate signal and optional JSON payload.
6. Clean up old run state when the workflow is complete.

## Secrets and payload handling

Procedo runtime does not print resume payload values directly.

Structured execution events now redact:

- any `payload` output branch emitted by wait/resume steps
- common sensitive key names such as `token`, `secret`, `password`, `api_key`, and `client_secret`

Recommendation:

- keep resume payloads minimal
- avoid placing secrets directly in workflow YAML
- treat persisted run files as operational data and protect access to the state directory

## Key environment variables

- `PROCEDO_WORKFLOW_PATH`
- `PROCEDO_STATE_DIR`
- `PROCEDO_PERSIST`
- `PROCEDO_MAX_PARALLELISM`
- `PROCEDO_DEFAULT_RETRIES`
- `PROCEDO_DEFAULT_TIMEOUT_MS`
- `PROCEDO_CONTINUE_ON_ERROR`
- `PROCEDO_RESUME_RUN_ID`
- `PROCEDO_RESUME_SIGNAL`
- `PROCEDO_RESUME_PAYLOAD_JSON`

## Inspect persisted runs

Use:

```powershell
dotnet run --project src/Procedo.Runtime -- --show-run <runId> --state-dir .procedo/runs
```

The command prints:

- run status
- source path when available
- timestamps
- waiting reason when applicable
- step counts
- per-step status summary

## Bulk cleanup

Use:

```powershell
# delete completed runs
dotnet run --project src/Procedo.Runtime -- --delete-completed --state-dir .procedo/runs

# delete failed runs
dotnet run --project src/Procedo.Runtime -- --delete-failed --state-dir .procedo/runs

# delete all persisted runs older than 30 minutes
dotnet run --project src/Procedo.Runtime -- --delete-all-older-than 00:30:00 --state-dir .procedo/runs

# delete waiting runs that have been paused for more than 2 hours
dotnet run --project src/Procedo.Runtime -- --delete-waiting-older-than 02:00:00 --state-dir .procedo/runs
```

Only one bulk delete filter can be used at a time.


