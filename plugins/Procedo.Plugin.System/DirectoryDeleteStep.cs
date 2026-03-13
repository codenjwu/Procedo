using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class DirectoryDeleteStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public DirectoryDeleteStep(SystemPluginSecurityOptions? securityOptions = null)
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

        var recursive = context.Inputs.TryGetValue("recursive", out var recursiveValue)
            ? SystemInputReader.GetBool(recursiveValue, true)
            : true;

        var ignoreMissing = context.Inputs.TryGetValue("ignore_missing", out var ignoreValue)
            ? SystemInputReader.GetBool(ignoreValue, true)
            : true;

        if (!Directory.Exists(path))
        {
            if (ignoreMissing)
            {
                return Task.FromResult(new StepResult
                {
                    Success = true,
                    Outputs = new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["deleted"] = false
                    }
                });
            }

            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Directory '{path}' does not exist."
            });
        }

        Directory.Delete(path, recursive);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = path,
                ["deleted"] = true
            }
        });
    }
}
