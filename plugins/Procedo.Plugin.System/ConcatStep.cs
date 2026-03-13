using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class ConcatStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var separator = context.Inputs.TryGetValue("separator", out var separatorValue)
            ? SystemInputReader.GetString(separatorValue)
            : string.Empty;

        IEnumerable<object> values;
        if (context.Inputs.TryGetValue("values", out var valuesValue))
        {
            values = SystemInputReader.GetValues(valuesValue);
        }
        else
        {
            var left = context.Inputs.TryGetValue("left", out var leftValue)
                ? SystemInputReader.GetString(leftValue)
                : string.Empty;

            var right = context.Inputs.TryGetValue("right", out var rightValue)
                ? SystemInputReader.GetString(rightValue)
                : string.Empty;

            values = new object[] { left, right };
        }

        var resultValue = string.Join(separator, values.Select(v => v?.ToString() ?? string.Empty));

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["value"] = resultValue
            }
        });
    }
}
