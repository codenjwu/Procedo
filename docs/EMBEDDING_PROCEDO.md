# Embedding Procedo

This guide shows how to embed Procedo into your own .NET application.

## When to embed Procedo

Embed Procedo when your application needs to:

- execute YAML-defined workflows locally
- register built-in or custom step types
- validate workflows before execution
- enable persistence/resume in a single-node host
- emit structured execution events into your application logging/telemetry flow
- support persistence-backed wait/resume flows while keeping operator diagnostics safe by default

Use `src/Procedo.Runtime` as the reference CLI host, but most applications should embed the library packages directly.

## Recommended package set

Most embedding scenarios only need:

- `Procedo.Engine`
- `Procedo.Hosting`
- `Procedo.Plugin.SDK`
- `Procedo.Plugin.System` if you want built-in `system.*` steps

Use `Procedo.Extensions.DependencyInjection` if your application already uses `IServiceCollection`.

## Minimal setup

Smallest useful host:

```csharp
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var yaml = await File.ReadAllTextAsync("examples/01_hello_echo.yaml");

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry =>
    {
        registry.AddSystemPlugin();
    })
    .Build();

var result = await host.ExecuteYamlAsync(yaml);
```

This path gives you YAML loading, validation, execution, and built-in system steps with very little setup.

## Event redaction

Structured execution events emitted through Procedo observability sinks automatically redact:

- any output branch named `payload`
- common sensitive key names such as `token`, `secret`, `password`, `api_key`, and `client_secret`

This is especially useful for wait/resume flows where a resume payload may be returned by a step for downstream logic but should not be written verbatim to logs or JSONL event sinks.

## Registration options

Procedo supports several ways to add custom behavior.

### 1. Direct `IProcedoStep` registration

Best for explicit reusable step implementations.

```csharp
registry.Register("custom.hello", () => new HelloStep());
```

### 2. Delegate registration

Best for small app-local logic.

```csharp
registry.Register("custom.hello", context => new StepResult
{
    Success = true,
    Outputs = new Dictionary<string, object>
    {
        ["message"] = $"Hello, {context.Inputs["name"]}"
    }
});
```

### 3. DI-backed step activation

Best for production apps where steps need services.

```csharp
registry.Register<MyStep>("custom.hello");
```

And on the host builder:

```csharp
var host = new ProcedoHostBuilder()
    .UseServiceProvider(serviceProvider)
    .ConfigurePlugins(registry => registry.Register<MyStep>("custom.hello"))
    .Build();
```

### 4. Method binding

Best when you want plain C# methods instead of step classes.

```csharp
registry.RegisterMethod("custom.summary", (Func<string, SummaryPayload>)BuildSummary);
```

## Method binding enhancements

Procedo method binding now supports:

- parameter aliases via `[StepInput("...")]`
- binding a complex POCO parameter from a nested input object
- binding a complex POCO parameter from the full flattened `with:` block when no direct input key exists
- explicit source attributes for context/services/logger/cancellation token
- clearer binding error messages that include parameter name, method name, and available input keys

### Parameter aliases

```csharp
static SummaryPayload BuildSummary([StepInput("user_name")] string name)
    => new($"Hello, {name}");
```

YAML:

```yaml
with:
  user_name: Procedo
```

### Flat POCO binding

```csharp
public sealed class PublishOptions
{
    public string Environment { get; set; } = string.Empty;
    public int RetryCount { get; set; }
}

static PublishPayload BuildPublish(PublishOptions options)
    => new($"{options.Environment}:{options.RetryCount}");
```

YAML:

```yaml
with:
  environment: production
  retryCount: 3
```

### Nested object binding

```csharp
static PublishPayload BuildPublish(PublishOptions options)
    => new($"{options.Environment}:{options.RetryCount}");
```

YAML:

```yaml
with:
  options:
    environment: production
    retryCount: 3
```

### Explicit source attributes

Use these when you want method signatures to be very explicit:

- `[FromStepContext]`
- `[FromServices]`
- `[FromLogger]`
- `[FromCancellationToken]`

Example:

```csharp
static SourcePayload Build(
    [FromStepContext] StepContext context,
    [FromServices] GreetingFormatter formatter,
    [FromLogger] ILogger logger,
    [FromCancellationToken] CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    logger.LogInformation("Building payload");
    return new($"{context.StepId}-{formatter.Format("Procedo")}");
}
```

See the runnable example:

- `examples/Procedo.Example.CustomSteps`
- `examples/41_custom_steps_inline_demo.yaml`

## Collision-safe registration

Procedo offers three registration semantics:

- `Register(...)`: overwrite existing registration for the same step type
- `TryRegister(...)`: keep the existing registration and return `false` on duplicate
- `RegisterOrThrow(...)`: fail fast on duplicate

Recommended production default:

- use `RegisterOrThrow(...)` during startup/bootstrap
- use `TryRegister(...)` when composing optional plugins
- use `Register(...)` only when override behavior is intentional

## Using the DI integration package

If your application already uses `Microsoft.Extensions.DependencyInjection`, use:

- `Procedo.Extensions.DependencyInjection`

Example:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Procedo.Engine.Hosting;
using Procedo.Extensions.DependencyInjection;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

var services = new ServiceCollection();
services.AddSingleton(new GreetingFormatter("DI builder"));

services.AddProcedo()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .RegisterStep<DiGreetingStep>("custom.di_greeting")
    .RegisterStep("custom.delegate_suffix", context => new StepResult
    {
        Success = true,
        Outputs = new Dictionary<string, object>
        {
            ["suffix"] = $"from {context.Inputs["environment"]}"
        }
    })
    .RegisterMethod("custom.compose_message", (Func<string, string, ComposedMessage>)ComposeMessage);

using var provider = services.BuildServiceProvider();
var host = provider.GetRequiredService<ProcedoHost>();
```

See the runnable example:

- `examples/Procedo.Example.DependencyInjection`
- `examples/42_dependency_injection_demo.yaml`

## Validation before execution

If you use `ProcedoHostBuilder`, validation runs by default before execution.

If you want an explicit parse/validate/execute flow, manual validation is still available.

The code below uses the `Procedo.DSL` and `Procedo.Validation` namespaces, but for package selection you should still follow the public package guidance above. You do not need to treat those as separate public packages.

```csharp
using Procedo.DSL;
using Procedo.Plugin.SDK;
using Procedo.Validation;
```

```csharp
var workflow = new YamlWorkflowParser().Parse(yaml);
IPluginRegistry registry = new PluginRegistry();
registry.AddSystemPlugin();

var validation = new ProcedoWorkflowValidator().Validate(workflow, registry);
if (validation.HasErrors)
{
    // report and stop
}
```

## Persistence and resume

For single-node persistence-backed execution:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs")
    .Build();
```

To resume a prior run:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs", resumeRunId: "run-123")
    .Build();
```

To resume a waiting run with a generic signal payload:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(".procedo/runs", resumeRunId: "run-123")
    .Build();

var resumed = await host.ResumeWorkflowAsync(workflow, new ResumeRequest
{
    SignalType = "continue",
    Payload = new Dictionary<string, object>
    {
        ["approved_by"] = "operator-1"
    }
});
```

A waiting step can inspect `StepContext.Resume` when it is re-executed after resume.

### Generic operator resume from the CLI host

The reference runtime host can resume waiting runs directly from the command line:

```powershell
dotnet run --project src/Procedo.Runtime -- examples/45_wait_signal_demo.yaml --resume run-123 --resume-signal continue --resume-payload-json "{""approved_by"":""operator""}"
```

For condition-based waiting steps like `system.wait_until` or `system.wait_file`, the signal acts as a re-check trigger. A simple value like `check` is enough.

### Listing waiting runs in the CLI host

For the reference runtime host, you can inspect persisted waiting runs without executing a workflow:

```powershell
dotnet run --project src/Procedo.Runtime -- --list-waiting --state-dir .procedo/runs
```

This prints the waiting run id, workflow name, waiting step, wait type, timestamp, and reason.

## Observability

Attach an execution event sink:

```csharp
var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseEventSink(new ConsoleExecutionEventSink())
    .Build();
```

Or combine sinks with a composite sink.

## Recommended production patterns

- keep step type names stable once published
- prefer `RegisterOrThrow(...)` during startup
- use DI-backed steps for external dependencies
- reserve delegate registration for small app-local logic
- use method binding when you want a lightweight facade over business methods, but keep signatures explicit
- use `[StepInput("...")]` when YAML naming differs from C# naming
- use explicit source attributes when method signatures mix workflow inputs and infrastructure dependencies
- move shared business steps into a dedicated plugin library when reuse starts growing
- keep step outputs JSON-friendly for persistence and observability
- treat `system.process_run` carefully and prefer explicit approval/allowlist patterns in your application

## Example map

Useful reference projects:

- `examples/Procedo.Example.Basic` for direct parser + validator + engine usage
- `examples/Procedo.Example.CustomSteps` for custom registration patterns
- `examples/Procedo.Example.DependencyInjection` for DI-based host composition
- `examples/Procedo.Example.PersistenceResume` for local persistence and resume
- `examples/Procedo.Example.Observability` for event sinks and traces

## Common pitfalls

- forgetting to register a step type before validation/execution
- accidentally overwriting an existing step type with `Register(...)`
- binding method parameters to YAML keys with mismatched names and not using `[StepInput("...")]`
- expecting complex POCO binding to work for collection-only or scalar-only parameter types
- using explicit source attributes on incompatible parameter types
- putting too much business logic into delegate-registered inline lambdas
- using reusable steps in app code for too long instead of moving them into a proper shared library




