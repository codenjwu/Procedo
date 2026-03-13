using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class GuidStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var format = context.Inputs.TryGetValue("format", out var formatValue)
            ? SystemInputReader.GetString(formatValue, "D")
            : "D";

        var uppercase = context.Inputs.TryGetValue("uppercase", out var uppercaseValue)
            && SystemInputReader.GetBool(uppercaseValue);

        var guidText = Guid.NewGuid().ToString(format);
        if (uppercase)
        {
            guidText = guidText.ToUpperInvariant();
        }

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["guid"] = guidText
            }
        });
    }
}
