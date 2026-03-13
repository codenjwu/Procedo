using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class FlakyStep : IProcedoStep
{
    private readonly ConcurrentDictionary<string, int> _invocationCounts;

    public FlakyStep(ConcurrentDictionary<string, int> invocationCounts)
    {
        _invocationCounts = invocationCounts;
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var key = $"{context.RunId}:{context.StepId}";
        var count = _invocationCounts.AddOrUpdate(key, 1, static (_, current) => current + 1);

        var failTimes = DemoInputReader.GetInt(context.Inputs.TryGetValue("fail_times", out var failValue) ? failValue : null, 1);
        var message = DemoInputReader.GetString(context.Inputs.TryGetValue("message", out var value) ? value : null, "flaky");

        if (count <= failTimes)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Transient failure {count}/{failTimes} for '{message}'."
            });
        }

        var outputs = new Dictionary<string, object>
        {
            ["message"] = message,
            ["attempt"] = count
        };

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = outputs
        });
    }
}
