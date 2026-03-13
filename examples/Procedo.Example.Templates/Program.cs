using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var workflowPath = args.Length > 0
    ? args[0]
    : Path.Combine(repoRoot, "examples", "48_template_parameters_demo.yaml");

var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
{
    ["environment"] = "prod",
    ["region"] = "westus"
};

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .Build();

var result = await host.ExecuteFileAsync(workflowPath, parameters).ConfigureAwait(false);

Console.WriteLine(result.Success
    ? $"Template run succeeded. RunId={result.RunId}"
    : $"Template run failed. [{result.ErrorCode}] {result.Error}");

return result.Success ? 0 : result.Waiting ? 2 : 1;

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
