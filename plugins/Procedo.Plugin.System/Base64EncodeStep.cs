using System.Text;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class Base64EncodeStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var text = context.Inputs.TryGetValue("text", out var textValue)
            ? SystemInputReader.GetString(textValue)
            : string.Empty;

        var encodingName = context.Inputs.TryGetValue("encoding", out var encodingValue)
            ? SystemInputReader.GetString(encodingValue, "utf8")
            : "utf8";

        var bytes = ResolveEncoding(encodingName).GetBytes(text);
        var encoded = Convert.ToBase64String(bytes);

        return Task.FromResult(new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["value"] = encoded
            }
        });
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
