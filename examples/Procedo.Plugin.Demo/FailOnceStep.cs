using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class FailOnceStep : IProcedoStep
{
    private readonly ConcurrentDictionary<string, int> _invocationCounts;

    public FailOnceStep(ConcurrentDictionary<string, int> invocationCounts)
    {
        _invocationCounts = invocationCounts;
    }

    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var key = $"{context.RunId}:{context.StepId}";
        var count = _invocationCounts.AddOrUpdate(key, 1, static (_, current) => current + 1);
        var message = DemoInputReader.GetString(context.Inputs.TryGetValue("message", out var value) ? value : null, "fail once");

        if (count == 1)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = message
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
