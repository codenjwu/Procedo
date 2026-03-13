using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class CsvWriteStep : IProcedoStep
{
    private readonly SystemSecurityGuard _securityGuard;

    public CsvWriteStep(SystemPluginSecurityOptions? securityOptions = null)
    {
        _securityGuard = new SystemSecurityGuard(securityOptions);
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        if (!context.Inputs.TryGetValue("rows", out var rowsValue))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Input 'rows' is required."
            });
        }

        var rows = new List<IDictionary<string, object>>();
        foreach (var item in SystemInputReader.GetValues(rowsValue))
        {
            var row = SystemInputReader.GetDictionary(item);
            if (row.Count > 0)
            {
                rows.Add(row);
            }
        }

        var delimiter = context.Inputs.TryGetValue("delimiter", out var delimiterValue)
            ? SystemInputReader.GetString(delimiterValue, ",")
            : ",";
        var includeHeader = context.Inputs.TryGetValue("include_header", out var headerValue)
            ? SystemInputReader.GetBool(headerValue, true)
            : true;

        var content = CsvUtility.Write(rows, delimiter[0], includeHeader);
        if (context.Inputs.TryGetValue("path", out var pathValue))
        {
            var path = SystemInputReader.GetString(pathValue);
            if (!string.IsNullOrWhiteSpace(path))
            {
                var pathError = _securityGuard.EnsurePathAllowed(path, "path");
                if (pathError is not null)
                {
                    return Task.FromResult(new StepResult { Success = false, Error = pathError });
                }

                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(path, content);
            }
        }

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["content"] = content,
                ["row_count"] = rows.Count
            }
        });
    }
}
