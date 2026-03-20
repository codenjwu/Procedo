using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

if (HasFlag(args, "--help", "-h"))
{
    PrintHelp();
    return 0;
}

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var options = ParseOptions(args);
var artifactsRoot = ResolvePath(GetOption(options, "artifacts-dir"), Path.Combine(repoRoot, ".procedo", "policy-host-artifacts"), repoRoot);
var allowedWorkflowPath = ResolvePath(GetOption(options, "allowed-workflow"), Path.Combine(repoRoot, "examples", "43_secure_runtime_allowed.yaml"), repoRoot);
var blockedWorkflowPath = ResolvePath(GetOption(options, "blocked-workflow"), Path.Combine(repoRoot, "examples", "44_secure_runtime_blocked_process.yaml"), repoRoot);
var outputPath = Path.Combine(artifactsRoot, "policy-host-demo.txt");

Directory.CreateDirectory(artifactsRoot);

var secureOptions = new SystemPluginSecurityOptions
{
    AllowHttpRequests = false,
    AllowFileSystemAccess = true,
    AllowProcessExecution = false,
    AllowUnsafeExecutables = false
};
secureOptions.AllowedPathRoots.Add(artifactsRoot);

var allowedYaml = (await File.ReadAllTextAsync(allowedWorkflowPath).ConfigureAwait(false))
    .Replace("__OUTPUT_PATH__", outputPath.Replace("\\", "/"), StringComparison.Ordinal);
var blockedYaml = await File.ReadAllTextAsync(blockedWorkflowPath).ConfigureAwait(false);

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(registry => registry.AddSystemPlugin(secureOptions))
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 2;
        execution.DefaultStepRetries = 1;
        execution.RetryInitialBackoffMs = 25;
        execution.RetryMaxBackoffMs = 100;
    })
    .ConfigureValidation(static validation => validation.TreatWarningsAsErrors = true)
    .Build();

var allowedResult = await host.ExecuteYamlAsync(allowedYaml).ConfigureAwait(false);
Console.WriteLine(allowedResult.Success
    ? $"Allowed workflow succeeded. RunId={allowedResult.RunId} Output={outputPath}"
    : $"Allowed workflow failed unexpectedly. [{allowedResult.ErrorCode}] {allowedResult.Error}");

var blockedResult = await host.ExecuteYamlAsync(blockedYaml).ConfigureAwait(false);
Console.WriteLine(!blockedResult.Success
    ? $"Blocked workflow failed as expected. [{blockedResult.ErrorCode}] {blockedResult.Error}"
    : "Blocked workflow unexpectedly succeeded.");

return allowedResult.Success && !blockedResult.Success ? 0 : 1;

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[arg[2..]] = args[++i];
        }
        else
        {
            options[arg[2..]] = null;
        }
    }

    return options;
}

static string? GetOption(IReadOnlyDictionary<string, string?> options, string name)
    => options.TryGetValue(name, out var value) ? value : null;

static bool HasFlag(string[] args, params string[] flags)
    => args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));

static string ResolvePath(string? value, string defaultPath, string repoRoot)
{
    var candidate = string.IsNullOrWhiteSpace(value) ? defaultPath : value;
    return Path.IsPathRooted(candidate) ? candidate : Path.GetFullPath(Path.Combine(repoRoot, candidate));
}

static void PrintHelp()
{
    Console.WriteLine("Procedo.Example.PolicyHost");
    Console.WriteLine();
    Console.WriteLine("Demonstrates host-level execution and security policy configuration.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.PolicyHost");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.PolicyHost -- --artifacts-dir .procedo/custom-policy --allowed-workflow examples/43_secure_runtime_allowed.yaml --blocked-workflow examples/44_secure_runtime_blocked_process.yaml");
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
