using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class ScoreStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var signal = DemoInputReader.GetString(context.Inputs.TryGetValue("message", out var value) ? value : null, context.StepId);

        var baseScore = Math.Abs(signal.GetHashCode()) % 40;
        var score = 60 + baseScore;

        var outputs = new Dictionary<string, object>
        {
            ["score"] = score,
            ["band"] = score >= 85 ? "A" : score >= 75 ? "B" : "C",
            ["signal"] = signal
        };

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = outputs
        });
    }
}
