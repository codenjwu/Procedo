# Plugin Authoring Contract

Procedo step plugins implement:

```csharp
public interface IProcedoStep
{
    Task<StepResult> ExecuteAsync(StepContext context);
}
```

## Contract expectations

- Plugins should respect `context.CancellationToken` when performing waits/IO.
- Plugins should be idempotent where possible (safe for retries).
- `StepResult.Success = true` on success.
- `StepResult.Success = false` with `Error` for recoverable failures.
- Return structured outputs via `StepResult.Outputs` for downstream expression binding.

## Registration modes

- Low-level contract: implement `IProcedoStep` directly.
- Delegate registration: `registry.Register("custom.x", ctx => ...)`.
- DI-backed activation: `registry.Register<MyStep>("custom.x")` plus `UseServiceProvider(...)`.
- Method binding: `registry.RegisterMethod("custom.x", (Func<...>)MyMethod)`.

These are additive convenience layers over the same runtime contract. The engine still executes resolved `IProcedoStep` instances.

## Method binding details

Method binding supports:

- input aliasing with `[StepInput("...")]`
- service injection by parameter type from `IServiceProvider`
- special parameter injection for `StepContext`, `CancellationToken`, `ILogger`, and `IServiceProvider`
- POCO binding from nested input objects
- POCO binding from the flattened `with:` block when no direct input key is present
- automatic conversion of scalar, dictionary, object, and async return values into `StepResult`

Prefer method binding when you want low ceremony, but keep method signatures explicit and stable.

## Registration collision behavior

- `Register(...)`: last write wins and replaces any existing registration for the same step type.
- `TryRegister(...)`: returns `false` if the step type already exists and leaves the existing registration untouched.
- `RegisterOrThrow(...)`: throws if the step type already exists.
- `Contains(...)`: checks whether a step type is already registered.

Convenience variants follow the same pattern:

- delegate overloads: `TryRegister(...)`, `RegisterOrThrow(...)`
- DI generic overloads: `TryRegister<TStep>(...)`, `RegisterOrThrow<TStep>(...)`
- method binding overloads: `TryRegisterMethod(...)`, `RegisterMethodOrThrow(...)`

## Direct registration vs plugin project

- Today, reusable custom step implementations should still implement `IProcedoStep` somewhere.
- A separate plugin project is optional.
- End-user applications can register step factories directly through `ProcedoHostBuilder.ConfigurePlugins(...)`.
- Use a dedicated plugin project only when you want reuse across apps, separate versioning, or NuGet distribution.

## Retry and timeout behavior

- Runtime may retry failed steps according to policy.
- Runtime may enforce timeout; cancellation token will be signaled.
- Plugins should avoid non-cancelable blocking operations.

## Logging

- Use `context.Logger` for structured operational logs.
- Include step-relevant context (input key names, external ids) without leaking secrets.

## Output compatibility

- Keep output keys stable once used by downstream expressions.
- Prefer simple JSON-compatible values for persistence/observability compatibility.

## Reference implementations

Use these built-in plugins as implementation references:

- `plugins/Procedo.Plugin.System/EchoStep.cs`: minimal success step with outputs.
- `plugins/Procedo.Plugin.System/GuidStep.cs`: ID generation and configurable output format.
- `plugins/Procedo.Plugin.System/NowStep.cs`: UTC timestamp and unix epoch outputs.
- `plugins/Procedo.Plugin.System/ConcatStep.cs`: deterministic string composition utility.
- `plugins/Procedo.Plugin.System/SleepStep.cs`: cancellation-aware wait pattern.
- `plugins/Procedo.Plugin.System/HttpStep.cs`: outbound HTTP call with headers/body/timeout and status handling.
- `plugins/Procedo.Plugin.System/FileWriteTextStep.cs`: write/append text to file with encoding options.
- `plugins/Procedo.Plugin.System/FileReadTextStep.cs`: read text file content and metadata.
- `plugins/Procedo.Plugin.System/FileCopyStep.cs`: copy file to target path.
- `plugins/Procedo.Plugin.System/FileMoveStep.cs`: move file with optional overwrite.
- `plugins/Procedo.Plugin.System/FileDeleteStep.cs`: delete file with optional ignore-missing behavior.
- `plugins/Procedo.Plugin.System/Base64EncodeStep.cs`: string-to-base64 utility.
- `plugins/Procedo.Plugin.System/Base64DecodeStep.cs`: base64-to-string utility.
- `plugins/Procedo.Plugin.System/HashStep.cs`: text/file hashing with common algorithms.
- `plugins/Procedo.Plugin.System/ZipCreateStep.cs`: create zip archives from a directory.
- `plugins/Procedo.Plugin.System/ZipExtractStep.cs`: extract zip archives into a target directory.
- `plugins/Procedo.Plugin.System/DirectoryCreateStep.cs`: create directories for later pipeline stages.
- `plugins/Procedo.Plugin.System/DirectoryListStep.cs`: enumerate files/directories with optional recursion.
- `plugins/Procedo.Plugin.System/DirectoryDeleteStep.cs`: remove directories with recursive/ignore-missing controls.
- `plugins/Procedo.Plugin.System/JsonGetStep.cs`: extract a JSON value by path.
- `plugins/Procedo.Plugin.System/JsonSetStep.cs`: mutate a JSON document at a path.
- `plugins/Procedo.Plugin.System/JsonMergeStep.cs`: merge two JSON payloads for downstream use.
- `plugins/Procedo.Plugin.System/ProcessRunStep.cs`: guarded process execution with timeout and blocked shell defaults.
- `plugins/Procedo.Plugin.System/CsvReadStep.cs`: parse CSV content/file into structured rows.
- `plugins/Procedo.Plugin.System/CsvWriteStep.cs`: write structured rows back to CSV content/file.
- `plugins/Procedo.Plugin.System/XmlGetStep.cs`: extract XML element/attribute values using a simple path syntax.
- `plugins/Procedo.Plugin.System/XmlSetStep.cs`: update XML element/attribute values and emit the mutated document.
- `examples/Procedo.Example.CustomSteps/Program.cs`: delegate registration, DI-backed step activation, method binding aliases, and POCO binding without a separate plugin project.
- `examples/Procedo.Plugin.Demo/FlakyStep.cs`: retry-oriented transient failure pattern.
- `examples/Procedo.Plugin.Demo/SleepStep.cs`: cancellation-aware wait/timeout pattern.
- `examples/Procedo.Plugin.Demo/QualityStep.cs`: deterministic pass/fail and structured outputs.
- `examples/Procedo.Plugin.Demo/FailOnceStep.cs`: persistence/resume-aware state behavior by `RunId + StepId`.
