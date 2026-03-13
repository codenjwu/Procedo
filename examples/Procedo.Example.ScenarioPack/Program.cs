using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

var workflows = args.Length > 0
    ? args.Select(path => Path.GetFullPath(path)).ToArray()
    : new[]
    {
        Path.Combine(repoRoot, "examples", "01_hello_echo.yaml"),
        Path.Combine(repoRoot, "examples", "07_job_max_parallelism.yaml"),
        Path.Combine(repoRoot, "examples", "24_end_to_end_reference.yaml"),
        Path.Combine(repoRoot, "examples", "25_data_platform_full_pipeline.yaml")
    };

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 6;
        execution.DefaultStepRetries = 2;
        execution.RetryInitialBackoffMs = 25;
        execution.RetryMaxBackoffMs = 300;
    })
    .Build();

var hasFailures = false;

foreach (var workflowPath in workflows)
{
    var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
    var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);

    Console.WriteLine($"{Path.GetFileName(workflowPath)} => {(result.Success ? "SUCCESS" : "FAILED")}");
    if (!result.Success)
    {
        Console.WriteLine($"  [{result.ErrorCode}] {result.Error}");
        hasFailures = true;
    }
}

return hasFailures ? 1 : 0;
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

