using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class JsonGetStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var json = context.Inputs.TryGetValue("json", out var jsonValue)
            ? SystemInputReader.GetString(jsonValue)
            : string.Empty;
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Inputs 'json' and 'path' are required."
            });
        }

        try
        {
            var root = JsonUtility.DeserializeToObject(json);
            var value = JsonUtility.GetValue(root, path);

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = value ?? string.Empty,
                    ["json"] = JsonUtility.Serialize(value)
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}
