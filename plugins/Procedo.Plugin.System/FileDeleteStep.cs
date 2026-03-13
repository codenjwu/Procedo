using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class FileDeleteStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public FileDeleteStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            return new StepResult
            {
                Success = false,
                Error = "Input 'path' is required."
            };
        }

        var pathError = _securityGuard.EnsurePathAllowed(path, "path");
        if (pathError is not null)
        {
            return new StepResult { Success = false, Error = pathError };
        }

        var ignoreMissing = context.Inputs.TryGetValue("ignore_missing", out var ignoreValue)
            ? SystemInputReader.GetBool(ignoreValue, true)
            : true;

        if (!File.Exists(path))
        {
            if (ignoreMissing)
            {
                return new StepResult
                {
                    Success = true,
                    Outputs = new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["deleted"] = false
                    }
                };
            }

            return new StepResult
            {
                Success = false,
                Error = $"File '{path}' does not exist."
            };
        }

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                File.Delete(path);

                return new StepResult
                {
                    Success = true,
                    Outputs = new Dictionary<string, object>
                    {
                        ["path"] = path,
                        ["deleted"] = true
                    }
                };
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                await Task.Delay(25 * attempt, context.CancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                await Task.Delay(25 * attempt, context.CancellationToken).ConfigureAwait(false);
            }
        }

        return new StepResult
        {
            Success = false,
            Error = $"File '{path}' could not be deleted after multiple attempts."
        };
    }
}
