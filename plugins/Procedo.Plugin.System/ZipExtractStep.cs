using System.IO.Compression;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class ZipExtractStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public ZipExtractStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var zipPath = context.Inputs.TryGetValue("zip_path", out var zipValue)
            ? SystemInputReader.GetString(zipValue)
            : string.Empty;

        var destinationDirectory = context.Inputs.TryGetValue("destination_directory", out var destinationValue)
            ? SystemInputReader.GetString(destinationValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(zipPath) || string.IsNullOrWhiteSpace(destinationDirectory))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Inputs 'zip_path' and 'destination_directory' are required."
            });
        }

        var zipPathError = _securityGuard.EnsurePathAllowed(zipPath, "zip_path");
        if (zipPathError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = zipPathError });
        }

        var destinationError = _securityGuard.EnsurePathAllowed(destinationDirectory, "destination_directory");
        if (destinationError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = destinationError });
        }

        if (!File.Exists(zipPath))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Zip file '{zipPath}' does not exist."
            });
        }

        var overwrite = context.Inputs.TryGetValue("overwrite", out var overwriteValue)
            ? SystemInputReader.GetBool(overwriteValue, true)
            : true;

        Directory.CreateDirectory(destinationDirectory);
        var destinationRoot = Path.GetFullPath(destinationDirectory);
        if (!destinationRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            destinationRoot += Path.DirectorySeparatorChar;
        }

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in archive.Entries)
            {
                var fullPath = Path.GetFullPath(Path.Combine(destinationRoot, entry.FullName));
                if (!fullPath.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new StepResult
                    {
                        Success = false,
                        Error = $"Archive entry '{entry.FullName}' would extract outside destination directory."
                    });
                }

                var fullPathError = _securityGuard.EnsurePathAllowed(fullPath, $"entry:{entry.FullName}");
                if (fullPathError is not null)
                {
                    return Task.FromResult(new StepResult { Success = false, Error = fullPathError });
                }

                var fullDirectory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrWhiteSpace(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    continue;
                }

                if (overwrite && File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                entry.ExtractToFile(fullPath);
            }
        }

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["zip_path"] = zipPath,
                ["destination_directory"] = destinationDirectory
            }
        });
    }
}
