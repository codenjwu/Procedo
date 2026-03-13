using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var examples = args.Length > 0
    ? args.Select(path => Path.IsPathRooted(path) ? path : Path.Combine(repoRoot, path)).ToArray()
    : new[]
    {
        Path.Combine(repoRoot, "examples", "58_runtime_expression_function_showcase.yaml"),
        Path.Combine(repoRoot, "examples", "59_branching_operator_showcase.yaml")
    };

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .Build();

var hasFailures = false;
foreach (var example in examples)
{
    Console.WriteLine($">>> {Path.GetFileName(example)}");
    var result = await host.ExecuteFileAsync(example).ConfigureAwait(false);
    Console.WriteLine(result.Success
        ? $"Success. RunId={result.RunId}"
        : $"Failed. [{result.ErrorCode}] {result.Error}");
    Console.WriteLine();

    if (!result.Success)
    {
        hasFailures = true;
    }
}

return hasFailures ? 1 : 0;

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
