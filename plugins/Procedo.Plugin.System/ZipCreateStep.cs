using System.IO.Compression;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class ZipCreateStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public ZipCreateStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var sourceDirectory = context.Inputs.TryGetValue("source_directory", out var sourceValue)
            ? SystemInputReader.GetString(sourceValue)
            : string.Empty;

        var zipPath = context.Inputs.TryGetValue("zip_path", out var zipValue)
            ? SystemInputReader.GetString(zipValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(zipPath))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Inputs 'source_directory' and 'zip_path' are required."
            });
        }

        var sourceError = _securityGuard.EnsurePathAllowed(sourceDirectory, "source_directory");
        if (sourceError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = sourceError });
        }

        var zipPathError = _securityGuard.EnsurePathAllowed(zipPath, "zip_path");
        if (zipPathError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = zipPathError });
        }

        if (!Directory.Exists(sourceDirectory))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Directory '{sourceDirectory}' does not exist."
            });
        }

        var overwrite = context.Inputs.TryGetValue("overwrite", out var overwriteValue)
            ? SystemInputReader.GetBool(overwriteValue, true)
            : true;

        var zipDirectory = Path.GetDirectoryName(zipPath);
        if (!string.IsNullOrWhiteSpace(zipDirectory))
        {
            Directory.CreateDirectory(zipDirectory);
        }

        if (overwrite && File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(sourceDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["zip_path"] = zipPath,
                ["source_directory"] = sourceDirectory
            }
        });
    }
}
