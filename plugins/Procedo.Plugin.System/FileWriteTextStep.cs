using System.Text;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class FileWriteTextStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public FileWriteTextStep(SystemPluginSecurityOptions? securityOptions = null)
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

        var content = context.Inputs.TryGetValue("content", out var contentValue)
            ? SystemInputReader.GetString(contentValue)
            : string.Empty;

        var append = context.Inputs.TryGetValue("append", out var appendValue)
            ? SystemInputReader.GetBool(appendValue)
            : false;

        var createDirectory = context.Inputs.TryGetValue("create_directory", out var createDirValue)
            ? SystemInputReader.GetBool(createDirValue, true)
            : true;

        if (createDirectory)
        {
            var targetDir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
        }

        var encoding = ResolveEncoding(context.Inputs.TryGetValue("encoding", out var encodingValue)
            ? SystemInputReader.GetString(encodingValue, "utf8")
            : "utf8");

        if (append)
        {
            File.AppendAllText(path, content, encoding);
        }
        else
        {
            File.WriteAllText(path, content, encoding);
        }

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = path,
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
