using System.Threading.Tasks;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public sealed class SleepStep : IProcedoStep
{
    public async Task<StepResult> ExecuteAsync(StepContext context)
    {
        var milliseconds = DemoInputReader.GetInt(context.Inputs.TryGetValue("milliseconds", out var value) ? value : null, 1000);
        if (milliseconds < 0)
        {
            milliseconds = 0;
        }

        await Task.Delay(milliseconds, context.CancellationToken).ConfigureAwait(false);

        return new StepResult
        {
            Success = true
        };
    }
}
