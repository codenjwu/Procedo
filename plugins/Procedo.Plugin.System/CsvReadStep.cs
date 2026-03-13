using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class CsvReadStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public CsvReadStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var content = context.Inputs.TryGetValue("content", out var contentValue)
            ? SystemInputReader.GetString(contentValue)
            : null;
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : null;

        if (string.IsNullOrWhiteSpace(content) && string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Either 'content' or 'path' is required."
            });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            var pathError = _securityGuard.EnsurePathAllowed(path!, "path");
            if (pathError is not null)
            {
                return Task.FromResult(new StepResult { Success = false, Error = pathError });
            }

            if (!File.Exists(path))
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = $"CSV file '{path}' does not exist."
                });
            }

            content = File.ReadAllText(path!);
        }

        var delimiter = context.Inputs.TryGetValue("delimiter", out var delimiterValue)
            ? SystemInputReader.GetString(delimiterValue, ",")
            : ",";
        var hasHeader = context.Inputs.TryGetValue("has_header", out var headerValue)
            ? SystemInputReader.GetBool(headerValue, true)
            : true;

        var rows = CsvUtility.Read(content!, delimiter[0], hasHeader);
        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["rows"] = rows,
                ["count"] = rows.Count
            }
        });
    }
}
