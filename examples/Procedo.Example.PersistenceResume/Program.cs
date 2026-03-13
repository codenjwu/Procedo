using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(repoRoot, "examples", "17_persistence_resume_after_failure.yaml");

var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
var stateDirectory = Path.Combine(repoRoot, ".procedo", "example-runs", "resume-demo");
Directory.CreateDirectory(stateDirectory);

var firstHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseLocalRunStateStore(stateDirectory)
    .Build();

var firstResult = await firstHost.ExecuteYamlAsync(yaml).ConfigureAwait(false);
Console.WriteLine($"First run  : Success={firstResult.Success} RunId={firstResult.RunId}");

if (firstResult.Success)
{
    Console.WriteLine("The sample workflow is expected to fail once before resume. No resume required.");
    return 0;
}

var resumeHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseLocalRunStateStore(stateDirectory, firstResult.RunId)
    .Build();

var resumedResult = await resumeHost.ExecuteYamlAsync(yaml).ConfigureAwait(false);
Console.WriteLine($"Resumed run: Success={resumedResult.Success} RunId={resumedResult.RunId}");

return resumedResult.Success ? 0 : 1;
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

