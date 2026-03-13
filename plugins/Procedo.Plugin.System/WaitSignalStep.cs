using Procedo.Core.Runtime;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class WaitSignalStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var expectedSignal = context.Inputs.TryGetValue("signal_type", out var signalValue)
            ? SystemInputReader.GetString(signalValue, "continue")
            : "continue";
        var reason = context.Inputs.TryGetValue("reason", out var reasonValue)
            ? SystemInputReader.GetString(reasonValue, "Waiting for external signal")
            : "Waiting for external signal";
        var waitKey = context.Inputs.TryGetValue("key", out var keyValue)
            ? SystemInputReader.GetString(keyValue, context.RunId)
            : context.RunId;

        var resume = context.Resume;
        if (resume is not null && string.Equals(resume.SignalType, expectedSignal, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["resumed"] = true,
                    ["signal_type"] = resume.SignalType ?? string.Empty,
                    ["payload"] = new Dictionary<string, object>(resume.Payload, StringComparer.OrdinalIgnoreCase)
                }
            });
        }

        return Task.FromResult(new StepResult
        {
            Waiting = true,
            Wait = new WaitDescriptor
            {
                Type = "signal",
                Reason = reason,
                Key = waitKey,
                Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["expected_signal_type"] = expectedSignal
                }
            }
        });
    }
}
