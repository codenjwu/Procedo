# Method Binding

Procedo method binding lets you register ordinary C# methods as workflow step implementations.

Example:

```csharp
registry.RegisterMethod("custom.summary", (Func<string, SummaryPayload>)BuildSummary);
```

At runtime, Procedo binds workflow inputs, selected infrastructure objects, and services to method parameters, invokes the method, and converts the return value into a `StepResult`.

## When to use method binding

Use method binding when:

- you want low ceremony for app-local logic
- the logic maps cleanly to a single method
- you want a lighter alternative to implementing `IProcedoStep`

Prefer `IProcedoStep` when:

- the logic is large or stateful
- you want an explicit reusable plugin contract
- the step has enough complexity to deserve its own type

## Registration

```csharp
registry.RegisterMethod("custom.summary", (Func<string, SummaryPayload>)BuildSummary);
```

Duplicate-safe variants:

- `TryRegisterMethod(...)`
- `RegisterMethodOrThrow(...)`

## Supported parameter sources

### Workflow inputs by name

If a parameter name matches an input key, it will bind automatically.

```csharp
static string BuildSummary(string name)
    => $"Hello, {name}";
```

YAML:

```yaml
with:
  name: Procedo
```

### Input aliases with `[StepInput("...")]`

Use when YAML naming differs from C# naming.

```csharp
static string BuildSummary([StepInput("user_name")] string name)
    => $"Hello, {name}";
```

YAML:

```yaml
with:
  user_name: Procedo
```

### Complex POCO binding from flat inputs

```csharp
public sealed class PublishOptions
{
    public string Environment { get; set; } = string.Empty;
    public int RetryCount { get; set; }
}

static PublishPayload Build(PublishOptions options)
    => new($"{options.Environment}:{options.RetryCount}");
```

YAML:

```yaml
with:
  environment: production
  retryCount: 3
```

### Complex POCO binding from nested input object

```yaml
with:
  options:
    environment: production
    retryCount: 3
```

### Explicit source attributes

Procedo supports these explicit source attributes:

- `[FromStepContext]`
- `[FromServices]`
- `[FromLogger]`
- `[FromCancellationToken]`

Example:

```csharp
static Payload Build(
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

## Built-in convention sources

Even without explicit attributes, Procedo can bind these special parameter types by convention:

- `StepContext`
- `CancellationToken`
- `ILogger`
- `IServiceProvider`
- `IDictionary<string, object>`

Explicit attributes are recommended when you want the source to be visually obvious in the method signature.

## Supported return shapes

Method return values are converted into `StepResult` like this:

- `StepResult` -> used directly
- `null` -> success with no outputs
- scalar (`string`, `int`, `bool`, etc.) -> output key `value`
- `IDictionary<string, object>` -> output dictionary
- object/record/POCO -> output properties become output keys
- `Task<T>` / `ValueTask<T>` -> awaited and converted
- `Task` / `ValueTask` -> awaited, success with no outputs

## Binding diagnostics

When binding fails, Procedo now reports:

- parameter name
- parameter type
- expected input name
- method name
- available input keys

This makes missing/misnamed inputs much easier to diagnose.

## Recommended usage guidelines

- keep method signatures explicit and small
- use `[StepInput("...")]` whenever YAML and C# naming differ
- use explicit source attributes when mixing workflow inputs and infrastructure services
- use POCO parameters for grouped business input
- avoid very large method signatures
- move complex logic into `IProcedoStep` classes when the method stops feeling simple

## Examples

Runnable example:

- `examples/Procedo.Example.CustomSteps`
- `examples/41_custom_steps_inline_demo.yaml`

Tests covering behavior:

- `tests/Procedo.UnitTests/PluginRegistrationModesTests.cs`
