using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? (Path.IsPathRooted(args[0]) ? args[0] : Path.Combine(repoRoot, args[0]))
    : Path.Combine(repoRoot, "examples", "62_template_multi_stage_promotion_demo.yaml");

var stateDirectory = Path.Combine(repoRoot, ".procedo", "example-multi-stage-promotion");
TryDeleteDirectory(stateDirectory);
Directory.CreateDirectory(stateDirectory);

var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);
workflow.Variables["workspace"] = stateDirectory;
workflow.Variables["request_dir"] = Path.Combine(stateDirectory, "request");
workflow.Variables["bundle_dir"] = Path.Combine(stateDirectory, "bundle");
workflow.Variables["output_dir"] = Path.Combine(stateDirectory, "output");

const string runId = "multi-stage-promotion-example";
var firstHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
    .Build();

var first = await firstHost.ExecuteWorkflowAsync(workflow).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");
if (!first.Waiting)
{
    return first.Success ? 0 : 1;
}

var resumeHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(stateDirectory, resumeRunId: runId)
    .Build();

var resumed = await resumeHost.ResumeWorkflowAsync(
    workflow,
    new ResumeRequest { SignalType = "approve" }).ConfigureAwait(false);

Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
return resumed.Success ? 0 : 1;

static void TryDeleteDirectory(string path)
{
    try { Directory.Delete(path, true); } catch { }
}

static string FindRepoRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Procedo.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root (Procedo.sln).");
}
