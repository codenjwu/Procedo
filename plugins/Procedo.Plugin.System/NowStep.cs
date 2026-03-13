using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class NowStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var format = context.Inputs.TryGetValue("format", out var formatValue)
            ? SystemInputReader.GetString(formatValue, "O")
            : "O";

        var outputs = new Dictionary<string, object>
        {
            ["utc"] = utcNow.ToString(format),
            ["unix_ms"] = utcNow.ToUnixTimeMilliseconds(),
            ["unix_s"] = utcNow.ToUnixTimeSeconds()
        };

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = outputs
        });
    }
}
