using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class SleepStep : IProcedoStep
{
    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        var milliseconds = context.Inputs.TryGetValue("milliseconds", out var msValue)
            ? Math.Max(0, SystemInputReader.GetInt(msValue))
            : 0;

        await Task.Delay(milliseconds, context.CancellationToken).ConfigureAwait(false);

        return new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["slept_ms"] = milliseconds
            }
        };
    }
}
