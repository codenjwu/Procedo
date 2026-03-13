using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var artifactsRoot = Path.Combine(repoRoot, ".procedo", "artifacts");
var allowedWorkflowPath = Path.Combine(repoRoot, "examples", "43_secure_runtime_allowed.yaml");
var blockedWorkflowPath = Path.Combine(repoRoot, "examples", "44_secure_runtime_blocked_process.yaml");
var outputPath = Path.Combine(artifactsRoot, "secure-runtime-demo.txt");

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

