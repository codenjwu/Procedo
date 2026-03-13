using System.Text;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class FileReadTextStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public FileReadTextStep(SystemPluginSecurityOptions? securityOptions = null)
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

        if (!File.Exists(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"File '{path}' does not exist."
            });
        }

        var encoding = ResolveEncoding(context.Inputs.TryGetValue("encoding", out var encodingValue)
            ? SystemInputReader.GetString(encodingValue, "utf8")
            : "utf8");

        var content = File.ReadAllText(path, encoding);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = path,
                ["content"] = content,
                ["length"] = content.Length
            }
        });
    }

    private static Encoding ResolveEncoding(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "utf8" => Encoding.UTF8,
            "unicode" or "utf16" => Encoding.Unicode,
            "ascii" => Encoding.ASCII,
            "utf32" => Encoding.UTF32,
            _ => Encoding.UTF8
        };
    }
}
