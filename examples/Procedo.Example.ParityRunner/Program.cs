using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
if (HasFlag(args, "--help", "-h"))
{
    PrintHelp(repoRoot);
    return 0;
}

var workflowArgument = GetWorkflowArgument(args);
var workflowPath = string.IsNullOrWhiteSpace(workflowArgument)
    ? Path.Combine(repoRoot, "examples", "66_retry_parity_demo.yaml")
    : (Path.IsPathRooted(workflowArgument) ? workflowArgument : Path.GetFullPath(Path.Combine(repoRoot, workflowArgument)));

var nonPersistedHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .Build();

var persistedStateDirectory = Path.Combine(repoRoot, ".procedo", "example-parity-runner");
TryDeleteDirectory(persistedStateDirectory);
Directory.CreateDirectory(persistedStateDirectory);

const string runId = "parity-runner-demo";
var persistedHost = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseLocalRunStateStore(persistedStateDirectory, runId)
    .Build();

var nonPersisted = await nonPersistedHost.ExecuteFileAsync(workflowPath).ConfigureAwait(false);
var persisted = await persistedHost.ExecuteFileAsync(workflowPath).ConfigureAwait(false);

Console.WriteLine($"Non-persisted: success={nonPersisted.Success}, waiting={nonPersisted.Waiting}, code={nonPersisted.ErrorCode}");
Console.WriteLine($"Persisted: success={persisted.Success}, waiting={persisted.Waiting}, code={persisted.ErrorCode}");

var parity = nonPersisted.Success == persisted.Success
    && nonPersisted.Waiting == persisted.Waiting
    && string.Equals(nonPersisted.ErrorCode, persisted.ErrorCode, StringComparison.Ordinal);

Console.WriteLine($"Parity match: {parity}");
return parity ? 0 : 1;

static bool HasFlag(string[] args, params string[] flags)
    => args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));

static string? GetWorkflowArgument(string[] args)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--workflow", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1];
        }

        if (!args[i].StartsWith("--", StringComparison.Ordinal))
        {
            return args[i];
        }
    }

    return null;
}

static void PrintHelp(string repoRoot)
{
    Console.WriteLine("Procedo.Example.ParityRunner");
    Console.WriteLine();
    Console.WriteLine("Runs the same workflow through non-persisted and persisted hosts and reports top-level parity.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.ParityRunner");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.ParityRunner -- --workflow examples/69_max_parallelism_parity_demo.yaml");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --workflow <path>  Workflow path. Defaults to examples/66_retry_parity_demo.yaml.");
    Console.WriteLine();
    Console.WriteLine($"Repo root: {repoRoot}");
}

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
