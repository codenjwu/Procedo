using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Procedo.Core.Models;

namespace Procedo.Core.Runtime;

public static class WorkflowDefinitionSnapshotCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(WorkflowDefinition workflow)
    {
        if (workflow is null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        return JsonSerializer.Serialize(workflow, JsonOptions);
    }

    public static WorkflowDefinition Deserialize(string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            throw new ArgumentException("A workflow snapshot is required.", nameof(snapshotJson));
        }

        var workflow = JsonSerializer.Deserialize<WorkflowDefinition>(snapshotJson, JsonOptions)
            ?? throw new InvalidDataException("Workflow snapshot did not contain a valid workflow definition.");

        NormalizeWorkflow(workflow);
        return workflow;
    }

    public static string ComputeFingerprint(string snapshotJson)
    {
        if (snapshotJson is null)
        {
            throw new ArgumentNullException(nameof(snapshotJson));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(snapshotJson));
        return Convert.ToHexString(bytes);
    }

    public static bool MatchesFingerprint(string snapshotJson, string? expectedFingerprint)
        => !string.IsNullOrWhiteSpace(expectedFingerprint)
           && string.Equals(ComputeFingerprint(snapshotJson), expectedFingerprint, StringComparison.OrdinalIgnoreCase);

    private static void NormalizeWorkflow(WorkflowDefinition workflow)
    {
        workflow.ParameterDefinitions ??= new Dictionary<string, ParameterDefinition>(StringComparer.OrdinalIgnoreCase);
        workflow.ParameterValues = NormalizeDictionary(workflow.ParameterValues);
        workflow.Variables = NormalizeDictionary(workflow.Variables);
        workflow.ParameterDefinitionSources ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        workflow.ParameterValueSources ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        workflow.VariableSources ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        workflow.Stages ??= new List<StageDefinition>();

        foreach (var stage in workflow.Stages)
        {
            stage.Jobs ??= new List<JobDefinition>();
            foreach (var job in stage.Jobs)
            {
                job.Steps ??= new List<StepDefinition>();
                foreach (var step in job.Steps)
                {
                    step.With = NormalizeDictionary(step.With);
                    step.DependsOn ??= new List<string>();
                }
            }
        }
    }

    private static Dictionary<string, object> NormalizeDictionary(IDictionary<string, object>? values)
    {
        var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (values is null)
        {
            return normalized;
        }

        foreach (var (key, value) in values)
        {
            normalized[key] = NormalizeValue(value)!;
        }

        return normalized;
    }

    private static object? NormalizeValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement element)
        {
            return ConvertElement(element);
        }

        if (value is IDictionary<string, object> typedDictionary)
        {
            return NormalizeDictionary(typedDictionary);
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                normalized[entry.Key?.ToString() ?? string.Empty] = NormalizeValue(entry.Value)!;
            }

            return normalized;
        }

        if (value is IEnumerable<object?> typedEnumerable && value is not string)
        {
            return typedEnumerable.Select(NormalizeValue).ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var normalized = new List<object?>();
            foreach (var item in enumerable)
            {
                normalized.Add(NormalizeValue(item));
            }

            return normalized;
        }

        return value;
    }

    private static object? ConvertElement(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i64) => i64,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                static p => p.Name,
                static p => ConvertElement(p.Value)!,
                StringComparer.OrdinalIgnoreCase),
            _ => element.ToString()
        };
}
