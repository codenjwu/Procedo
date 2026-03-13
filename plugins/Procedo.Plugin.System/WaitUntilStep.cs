using System.Globalization;
using Procedo.Core.Runtime;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class WaitUntilStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        if (!context.Inputs.TryGetValue("until_utc", out var untilValue))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "system.wait_until requires 'until_utc' in ISO-8601 UTC format."
            });
        }

        var raw = SystemInputReader.GetString(untilValue);
        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var untilUtc))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = $"Invalid until_utc value '{raw}'. Expected ISO-8601 date/time."
            });
        }

        var now = DateTimeOffset.UtcNow;
        if (now >= untilUtc)
        {
            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["until_utc"] = untilUtc.ToString("O", CultureInfo.InvariantCulture),
                    ["resumed_at_utc"] = now.ToString("O", CultureInfo.InvariantCulture)
                }
            });
        }

        return Task.FromResult(new StepResult
        {
            Waiting = true,
            Wait = new WaitDescriptor
            {
                Type = "time",
                Reason = $"Waiting until {untilUtc:O}",
                Key = untilUtc.ToString("O", CultureInfo.InvariantCulture),
                Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["until_utc"] = untilUtc.ToString("O", CultureInfo.InvariantCulture)
                }
            }
        });
    }
}
