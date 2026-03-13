using Procedo.DSL;
using Procedo.Engine;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;
using Procedo.Validation;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "24_end_to_end_reference.yaml");

var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
var workflow = new YamlWorkflowParser().Parse(yaml);

IPluginRegistry registry = new PluginRegistry();
registry.AddSystemPlugin();
registry.AddDemoPlugin();

var validation = new ProcedoWorkflowValidator().Validate(workflow, registry);
if (validation.HasErrors)
{
    Console.Error.WriteLine("Validation failed:");
    foreach (var issue in validation.Issues)
    {
        Console.Error.WriteLine($"- [{issue.Code}] {issue.Path}: {issue.Message}");
    }

    return 1;
}

var engine = new ProcedoWorkflowEngine();
var result = await engine.ExecuteAsync(workflow, registry, new ConsoleLogger()).ConfigureAwait(false);

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

