using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class JsonMergeStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var leftJson = context.Inputs.TryGetValue("left", out var leftValue)
            ? SystemInputReader.GetString(leftValue)
            : string.Empty;
        var rightJson = context.Inputs.TryGetValue("right", out var rightValue)
            ? SystemInputReader.GetString(rightValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(leftJson) || string.IsNullOrWhiteSpace(rightJson))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Inputs 'left' and 'right' are required."
            });
        }

        try
        {
            var left = JsonUtility.DeserializeToObject(leftJson);
            var right = JsonUtility.DeserializeToObject(rightJson);
            var merged = JsonUtility.Merge(left, right);

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["json"] = JsonUtility.Serialize(merged)
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
