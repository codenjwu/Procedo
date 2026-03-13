using System.Text;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class Base64DecodeStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var encoded = context.Inputs.TryGetValue("base64", out var encodedValue)
            ? SystemInputReader.GetString(encodedValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(encoded))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Input 'base64' is required."
            });
        }

        try
        {
            var encodingName = context.Inputs.TryGetValue("encoding", out var encodingValue)
                ? SystemInputReader.GetString(encodingValue, "utf8")
                : "utf8";

            var bytes = Convert.FromBase64String(encoded);
            var decoded = ResolveEncoding(encodingName).GetString(bytes);

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = decoded
                }
            });
        }
        catch (FormatException ex)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }

    private static Encoding ResolveEncoding(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "utf8" => Encoding.UTF8,
            "unicode" or "utf16" => Encoding.Unicode,
            "ascii" => Encoding.ASCII,
            "utf32" => Encoding.UTF32,
            _ => Encoding.UTF8
        };
    }
}
