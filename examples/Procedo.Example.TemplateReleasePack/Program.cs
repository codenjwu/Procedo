using Procedo.Engine.Hosting;
using Procedo.DSL;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? (Path.IsPathRooted(args[0]) ? args[0] : Path.Combine(repoRoot, args[0]))
    : Path.Combine(repoRoot, "examples", "60_template_branching_release_pack_demo.yaml");

var workspace = Path.Combine(repoRoot, ".procedo", "example-template-release-pack");
TryDeleteDirectory(workspace);

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .Build();

var workflow = new WorkflowTemplateLoader().LoadFromFile(workflowPath);
workflow.Variables["workspace"] = workspace;
workflow.Variables["handoff_dir"] = Path.Combine(workspace, "handoff");
workflow.Variables["output_dir"] = Path.Combine(workspace, "output");

var result = await host.ExecuteWorkflowAsync(workflow).ConfigureAwait(false);

Console.WriteLine(result.Success
    ? $"Release-pack example succeeded. RunId={result.RunId}"
    : $"Release-pack example failed. [{result.ErrorCode}] {result.Error}");

return result.Success ? 0 : 1;

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
