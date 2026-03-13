using System.Xml.Linq;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class XmlGetStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var xml = context.Inputs.TryGetValue("xml", out var xmlValue)
            ? SystemInputReader.GetString(xmlValue)
            : string.Empty;
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : string.Empty;

        if (string.IsNullOrWhiteSpace(xml) || string.IsNullOrWhiteSpace(path))
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = "Inputs 'xml' and 'path' are required."
            });
        }

        try
        {
            var document = XDocument.Parse(xml);
            var value = XmlUtility.GetValue(document, path) ?? string.Empty;

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = value
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new StepResult
            {
                Success = false,
                Error = ex.Message
            });
        }
    }
}
