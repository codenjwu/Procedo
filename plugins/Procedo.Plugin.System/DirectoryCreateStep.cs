using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class DirectoryCreateStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public DirectoryCreateStep(SystemPluginSecurityOptions? securityOptions = null)
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

        Directory.CreateDirectory(path);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = path,
                ["exists"] = Directory.Exists(path)
            }
        });
    }
}
