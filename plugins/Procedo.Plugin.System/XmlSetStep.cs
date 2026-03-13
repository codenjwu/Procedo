using System.Xml.Linq;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.System;

public sealed class XmlSetStep : IProcedoStep
{
    public Task<StepResult> ExecuteAsync(StepContext context)
    {
        var xml = context.Inputs.TryGetValue("xml", out var xmlValue)
            ? SystemInputReader.GetString(xmlValue)
            : string.Empty;
        var path = context.Inputs.TryGetValue("path", out var pathValue)
            ? SystemInputReader.GetString(pathValue)
            : string.Empty;
        var value = context.Inputs.TryGetValue("value", out var inputValue)
            ? SystemInputReader.GetString(inputValue)
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
            var updated = XmlUtility.SetValue(document, path, value);
            if (!updated)
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = $"Unable to set path '{path}'."
                });
            }

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["xml"] = document.ToString(SaveOptions.DisableFormatting)
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
