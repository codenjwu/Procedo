using Procedo.Core.Runtime;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class WaitFileStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public WaitFileStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        if (!context.Inputs.TryGetValue("path", out var pathValue))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "system.wait_file requires 'path'."
            });
        }

        var path = Path.GetFullPath(SystemInputReader.GetString(pathValue));
        var securityError = _securityGuard.EnsurePathAllowed(path, "path");
        if (securityError is not null)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = securityError
            });
        }

        if (File.Exists(path))
        {
            var info = new FileInfo(path);
            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["path"] = path,
                    ["exists"] = true,
                    ["length"] = info.Length,
                    ["last_write_utc"] = info.LastWriteTimeUtc.ToString("O", global::System.Globalization.CultureInfo.InvariantCulture)
                }
            });
        }

        var reason = context.Inputs.TryGetValue("reason", out var reasonValue)
            ? SystemInputReader.GetString(reasonValue, $"Waiting for file '{path}'")
            : $"Waiting for file '{path}'";

        return Task.FromResult(new StepResult
        {
            Waiting = true,
            Wait = new WaitDescriptor
            {
                Type = "file",
                Reason = reason,
                Key = path,
                Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["path"] = path
                }
            }
        });
    }
}
