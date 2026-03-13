using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class EchoStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var message = context.Inputs.TryGetValue("message", out var value)
            ? SystemInputReader.GetString(value)
            : string.Empty;

        Console.WriteLine(message);

        var outputs = new Dictionary<string, object>
        {
            ["message"] = message
        };

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = outputs
        });
    }
}
