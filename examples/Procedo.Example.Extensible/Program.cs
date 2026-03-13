using Procedo.Engine.Hosting;
using Procedo.Observability.Sinks;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "09_retry_transient.yaml");

var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseStrictValidation()
    .UseConsoleEvents()
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 4;
        execution.DefaultStepRetries = 2;
        execution.RetryInitialBackoffMs = 50;
        execution.RetryMaxBackoffMs = 250;
    })
    .Build();

var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);

Console.WriteLine(result.Success
    ? $"Run succeeded. RunId={result.RunId}"
    : $"Run failed. [{result.ErrorCode}] {result.Error}");

return result.Success ? 0 : 1;
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

