using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Observability;
using Procedo.Observability.Sinks;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(repoRoot, "examples", "46_wait_resume_observability.yaml");
var stateDirectory = Path.Combine(repoRoot, ".procedo", "wait-resume-observability");
var eventPath = Path.Combine(repoRoot, ".procedo", "events", "wait-resume-observability.jsonl");
const string runId = "wait-observability-run";

Directory.CreateDirectory(stateDirectory);
Directory.CreateDirectory(Path.GetDirectoryName(eventPath)!);
var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
var sink = new CompositeExecutionEventSink(new IExecutionEventSink[]
{
    new ConsoleExecutionEventSink(),
    new JsonFileExecutionEventSink(eventPath)
});

var firstHost = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin())
    .UseEventSink(sink)
    .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
    .Build();

var first = await firstHost.ExecuteYamlAsync(yaml).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");

if (first.Waiting)
{
    var resumeHost = new ProcedoHostBuilder()
        .ConfigurePlugins(registry => registry.AddSystemPlugin())
        .UseEventSink(sink)
        .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
        .Build();

    var resumed = await resumeHost.ResumeYamlAsync(yaml, new ResumeRequest
    {
        SignalType = "continue",
        Payload = new Dictionary<string, object>
        {
            ["source"] = "observability-demo"
        }
    }).ConfigureAwait(false);

    Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
}

Console.WriteLine($"Events file: {eventPath}");
return 0;

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
