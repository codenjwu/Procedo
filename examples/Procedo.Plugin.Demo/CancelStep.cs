using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class CancelStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var message = DemoInputReader.GetString(context.Inputs.TryGetValue("message", out var value) ? value : null, "Cancellation requested by demo.cancel.");

        return Task.FromResult(new StepResult
        {
            Success = false,
            Error = message
        });
    }
}
