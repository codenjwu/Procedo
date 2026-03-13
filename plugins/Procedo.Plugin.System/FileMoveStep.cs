using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class FileMoveStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public FileMoveStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var source = context.Inputs.TryGetValue("source", out var sourceValue)
            ? SystemInputReader.GetString(sourceValue)
            : string.Empty;

        var target = context.Inputs.TryGetValue("target", out var targetValue)
            ? SystemInputReader.GetString(targetValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Both 'source' and 'target' are required."
            });
        }

        var sourceError = _securityGuard.EnsurePathAllowed(source, "source");
        if (sourceError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = sourceError });
        }

        var targetError = _securityGuard.EnsurePathAllowed(target, "target");
        if (targetError is not null)
        {
            return Task.FromResult(new StepResult { Success = false, Error = targetError });
        }

        if (!File.Exists(source))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Source file '{source}' does not exist."
            });
        }

        var overwrite = context.Inputs.TryGetValue("overwrite", out var overwriteValue)
            ? SystemInputReader.GetBool(overwriteValue, false)
            : false;

        var createDirectory = context.Inputs.TryGetValue("create_directory", out var createDirValue)
            ? SystemInputReader.GetBool(createDirValue, true)
            : true;

        if (createDirectory)
        {
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }
        }

        if (overwrite && File.Exists(target))
        {
            File.Delete(target);
        }

        File.Move(source, target);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["source"] = source,
                ["target"] = target
            }
        });
    }
}
