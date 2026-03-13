using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "45_wait_signal_demo.yaml");
var stateDirectory = Path.Combine(repoRoot, ".procedo", "wait-resume-demo");
const string runId = "wait-demo-run";

Directory.CreateDirectory(stateDirectory);
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);

var firstHost = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
    .Build();

var first = await firstHost.ExecuteYamlAsync(yaml).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");
if (first.Waiting)
{
    Console.WriteLine($"Paused at step '{first.WaitingStepId}' with wait type '{first.WaitingType}'.");
}

if (!first.Waiting)
{
    return first.Success ? 0 : 1;
}

var resumeHost = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
    .Build();

var resumed = await resumeHost.ResumeYamlAsync(yaml, new ResumeRequest
{
    SignalType = "continue",
    Payload = new Dictionary<string, object>
    {
        ["approved_by"] = Environment.UserName
    }
}).ConfigureAwait(false);

Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
return resumed.Success ? 0 : 1;

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
