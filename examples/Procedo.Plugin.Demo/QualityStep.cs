using System.Collections.Generic;
using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class QualityStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var subject = DemoInputReader.GetString(context.Inputs.TryGetValue("message", out var value) ? value : null, "quality");
        var score = DemoInputReader.GetInt(context.Inputs.TryGetValue("score", out var scoreValue) ? scoreValue : null, 95);
        var threshold = DemoInputReader.GetInt(context.Inputs.TryGetValue("threshold", out var thresholdValue) ? thresholdValue : null, 80);

        var passed = score >= threshold;
        var outputs = new Dictionary<string, object>
        {
            ["subject"] = subject,
            ["score"] = score,
            ["threshold"] = threshold,
            ["passed"] = passed
        };

        return Task.FromResult(new StepResult
        {
            Success = passed,
            Error = passed ? null : $"Quality gate failed for '{subject}': score {score} < threshold {threshold}.",
            Outputs = outputs
        });
    }
}
