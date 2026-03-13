# Observability

Procedo emits structured execution events during workflow execution.

For most embedders, observability is part of the `Procedo.Engine` package story. You attach sinks through the hosting/runtime APIs rather than treating observability as a separate public package choice.

## Event Types

- `RunStarted`
- `RunCompleted`
- `RunFailed`
- `StepStarted`
- `StepCompleted`
- `StepFailed`
- `StepSkipped`
- `StepWaiting`
- `RunWaiting`
- `RunResumed`

## Event Schema (`SchemaVersion = 1`)

`ExecutionEvent` fields:

- `Sequence` (`long`): monotonic, per publisher.
- `TimestampUtc` (`DateTimeOffset`): publish timestamp.
- `EventType` (`ExecutionEventType`): event kind.
- `SchemaVersion` (`int`): schema contract version.
- `RunId` (`string`): workflow run identifier.
- `WorkflowName` (`string?`): workflow name.
- `Stage` (`string?`): stage name for step events.
- `Job` (`string?`): job name for step events.
- `StepId` (`string?`): step id for step events.
- `StepType` (`string?`): plugin step type for step events.
- `Success` (`bool?`): success indicator when applicable.
- `Resumed` (`bool?`): resume context (run-level and skipped steps).
- `WaitType` (`string?`): wait category for waiting events.
- `WaitKey` (`string?`): wait correlation key when available.
- `SignalType` (`string?`): resume signal type when available.
- `DurationMs` (`long?`): run/step duration when applicable.
- `Error` (`string?`): error text when failed.
- `SourcePath` (`string?`): originating workflow/template source path when available.
- `Outputs` (`Dictionary<string, object>?`): step outputs when available.

`SourcePath` is additive and is most useful for template-driven failures. `RunFailed` and `StepFailed` events now include it when Procedo can attribute the failing graph or step back to a source file.

## Sinks

`IExecutionEventSink` is pluggable.

Built-in sinks:

- `NullExecutionEventSink`
- `ConsoleExecutionEventSink`
- `JsonFileExecutionEventSink` (JSONL)
- `CompositeExecutionEventSink`

Sink failures are isolated and do not fail workflow execution.

Common embedding path:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseEventSink(new ConsoleExecutionEventSink())
    .Build();
```

## Runtime Flags

- `--events-console`: print structured events to console.
- `--events-json <path>`: append structured events to JSONL file.

You can use both flags together.

## Compatibility Strategy

- Snapshot tests guard serialized schema stability.
- Legacy payload tests ensure backward deserialization compatibility.
- Unknown-field tests ensure forward compatibility.
- Cross-target contract tests run on `net6.0`, `net8.0`, `net10.0`.
