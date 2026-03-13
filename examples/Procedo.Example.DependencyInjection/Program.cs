using Microsoft.Extensions.DependencyInjection;
using Procedo.Engine.Hosting;
using Procedo.Extensions.DependencyInjection;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "42_dependency_injection_demo.yaml");
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);

var services = new ServiceCollection();
services.AddSingleton(new GreetingFormatter("DI builder"));
services.AddProcedo()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .RegisterStep<DiGreetingStep>("custom.di_greeting")
    .RegisterStep("custom.delegate_suffix", static context => new StepResult
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
var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);

Console.WriteLine(result.Success
    ? $"Run succeeded. RunId={result.RunId}"
    : $"Run failed. [{result.ErrorCode}] {result.Error}");

return result.Success ? 0 : 1;

static ComposedMessage ComposeMessage(string greeting, string suffix)
    => new($"{greeting} {suffix}");

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

public sealed class DiGreetingStep : IProcedoStep
{
    private readonly GreetingFormatter _formatter;

    public DiGreetingStep(GreetingFormatter formatter)
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

public sealed record ComposedMessage(string Message);
