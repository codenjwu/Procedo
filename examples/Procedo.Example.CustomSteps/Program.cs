using Procedo.Engine.Hosting;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "41_custom_steps_inline_demo.yaml");
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);

var serviceProvider = new ExampleServiceProvider(new Dictionary<Type, object>
{
    [typeof(GreetingFormatter)] = new GreetingFormatter("DI")
});

var host = new ProcedoHostBuilder()
    .UseServiceProvider(serviceProvider)
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();

        registry.Register("custom.delegate_hello", context => new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["greeting"] = $"Delegate hello, {context.Inputs["name"]}",
                ["name"] = context.Inputs["name"]
            }
        });

        registry.Register<DiHelloStep>("custom.di_hello");
        registry.RegisterMethod("custom.method_summary", (Func<string, string, PublishOptions, SummaryPayload>)BuildSummary);
        registry.RegisterMethod("custom.method_sources", (Func<StepContext, GreetingFormatter, ILogger, CancellationToken, SourceSummaryPayload>)BuildSourceSummary);
    })
    .Build();

var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);

Console.WriteLine(result.Success
    ? $"Run succeeded. RunId={result.RunId}"
    : $"Run failed. [{result.ErrorCode}] {result.Error}");

return result.Success ? 0 : 1;

static SummaryPayload BuildSummary(
    [StepInput("delegate_greeting")] string delegateGreetingText,
    [StepInput("di_greeting")] string diGreetingText,
    PublishOptions publish)
    => new(
        $"{delegateGreetingText} | {diGreetingText} | env={publish.Environment} retries={publish.RetryCount}",
        delegateGreetingText.Length + diGreetingText.Length);

static SourceSummaryPayload BuildSourceSummary(
    [FromStepContext] StepContext context,
    [FromServices] GreetingFormatter formatter,
    [FromLogger] ILogger logger,
    [FromCancellationToken] CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    logger.LogInformation("Building source summary from explicit binding attributes.");
    return new SourceSummaryPayload($"source-step={context.StepId}; formatter={formatter.Format("Procedo")}");
}

static string FindRepoRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        var slnPath = Path.Combine(current.FullName, "Procedo.sln");
        if (File.Exists(slnPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root (Procedo.sln).");
}

public sealed class DiHelloStep : IProcedoStep
{
    private readonly GreetingFormatter _formatter;

    public DiHelloStep(GreetingFormatter formatter)
    {
        _formatter = formatter;
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var name = context.Inputs.TryGetValue("name", out var value)
            ? value?.ToString() ?? "world"
            : "world";

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["greeting"] = _formatter.Format(name)
            }
        });
    }
}

public sealed class GreetingFormatter
{
    private readonly string _prefix;

    public GreetingFormatter(string prefix)
    {
        _prefix = prefix;
    }

    public string Format(string name) => $"{_prefix} hello, {name}";
}

public sealed class PublishOptions
{
    public string Environment { get; set; } = string.Empty;

    public int RetryCount { get; set; }
}

public sealed record SummaryPayload(string Summary, int TotalLength);
public sealed record SourceSummaryPayload(string Summary);

public sealed class ExampleServiceProvider : IServiceProvider
{
    private readonly IReadOnlyDictionary<Type, object> _services;

    public ExampleServiceProvider(IReadOnlyDictionary<Type, object> services)
    {
        _services = services;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var service) ? service : null;
}
