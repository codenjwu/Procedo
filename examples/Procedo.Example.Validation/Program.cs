using Procedo.DSL;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;
using Procedo.Validation;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

var workflows = args.Length > 0
    ? args.Select(path => Path.GetFullPath(path)).ToArray()
    : new[]
    {
        Path.Combine(repoRoot, "examples", "13_missing_plugin_validation_error.yaml"),
        Path.Combine(repoRoot, "examples", "14_cycle_dependency_validation_error.yaml"),
        Path.Combine(repoRoot, "examples", "15_unknown_dependency_validation_error.yaml"),
        Path.Combine(repoRoot, "examples", "24_end_to_end_reference.yaml")
    };

var parser = new YamlWorkflowParser();
var validator = new ProcedoWorkflowValidator();
IPluginRegistry registry = new PluginRegistry();
registry.AddSystemPlugin();
registry.AddDemoPlugin();

var hasErrors = false;
foreach (var workflowPath in workflows)
{
    var yaml = await File.ReadAllTextAsync(workflowPath).ConfigureAwait(false);
    var workflow = parser.Parse(yaml);
    var result = validator.Validate(workflow, registry);

    Console.WriteLine($"Workflow: {Path.GetFileName(workflowPath)}");

    if (!result.Issues.Any())
    {
        Console.WriteLine("  - No validation issues.");
        continue;
    }

    foreach (var issue in result.Issues)
    {
        Console.WriteLine($"  - [{issue.Severity}] [{issue.Code}] {issue.Path}: {issue.Message}");
    }

    if (result.HasErrors)
    {
        hasErrors = true;
    }
}

return hasErrors ? 2 : 0;
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

