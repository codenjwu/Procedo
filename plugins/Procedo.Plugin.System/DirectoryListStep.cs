using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class DirectoryListStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public DirectoryListStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Input 'path' is required."
            });
        }

        var pathError = _securityGuard.EnsurePathAllowed(path, "path");
        if (pathError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = pathError });
        }

        if (!Directory.Exists(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Directory '{path}' does not exist."
            });
        }

        var recursive = context.Inputs.TryGetValue("recursive", out var recursiveValue)
            ? SystemInputReader.GetBool(recursiveValue)
            : false;

        var mode = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(path, "*", mode);
        var directories = Directory.GetDirectories(path, "*", mode);
        var items = files.Cast<object>().Concat(directories).ToArray();

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = path,
                ["files"] = files,
                ["directories"] = directories,
                ["items"] = items,
                ["file_count"] = files.Length,
                ["directory_count"] = directories.Length,
                ["count"] = items.Length
            }
        });
    }
}
