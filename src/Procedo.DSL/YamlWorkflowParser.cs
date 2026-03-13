using System;
using System.Collections;
using System.Collections.Generic;
using Procedo.Core.Abstractions;
using Procedo.Core.Models;
using Procedo.DSL.Parsing;

namespace Procedo.DSL;

public sealed class YamlWorkflowParser : IWorkflowParser
{
    public WorkflowDefinition Parse(string yamlText)
    {
        if (string.IsNullOrWhiteSpace(yamlText))
        {
            throw new ArgumentException("YAML content is empty.", nameof(yamlText));
        }

        var root = MinimalYamlParser.Parse(yamlText);
        if (root is not Dictionary<string, object?> rawMap)
        {
            throw new InvalidOperationException("Invalid workflow root document.");
        }

        var map = ConditionalYamlPreprocessor.ProcessRoot(rawMap);

        var workflow = new WorkflowDefinition
        {
            Name = GetString(map, "name"),
            Version = GetInt(map, "version", 1),
            Template = GetOptionalString(map, "template"),
            MaxParallelism = GetNullableInt(map, "max_parallelism"),
            ContinueOnError = GetNullableBool(map, "continue_on_error")
        };

        ParseParameters(map, workflow);
        ParseVariables(map, workflow);
        ParseStages(map, workflow);

        return workflow;
    }

    private static void ParseParameters(IDictionary<string, object?> map, WorkflowDefinition workflow)
    {
        if (!map.TryGetValue("parameters", out var parametersObj) || parametersObj is not Dictionary<string, object?> parametersMap)
        {
            return;
        }

        foreach (var (name, value) in parametersMap)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (value is Dictionary<string, object?> parameterMap && LooksLikeParameterDefinition(parameterMap))
            {
                workflow.ParameterDefinitions[name] = new ParameterDefinition
                {
                    Type = GetString(parameterMap, "type", "string"),
                    Required = GetBool(parameterMap, "required", false),
                    Default = parameterMap.TryGetValue("default", out var defaultValue) ? NormalizeValue(defaultValue) : null,
                    Description = GetOptionalString(parameterMap, "description"),
                    AllowedValues = GetObjectList(parameterMap, "allowed_values"),
                    Minimum = GetNullableDouble(parameterMap, "min"),
                    Maximum = GetNullableDouble(parameterMap, "max"),
                    MinLength = GetNullableInt(parameterMap, "min_length"),
                    MaxLength = GetNullableInt(parameterMap, "max_length"),
                    Pattern = GetOptionalString(parameterMap, "pattern"),
                    ItemType = GetOptionalString(parameterMap, "item_type"),
                    RequiredProperties = ToStringList(parameterMap.TryGetValue("required_properties", out var requiredProps) ? requiredProps : null).ToList()
                };
                continue;
            }

            workflow.ParameterValues[name] = NormalizeValue(value);
        }
    }

    private static void ParseVariables(IDictionary<string, object?> map, WorkflowDefinition workflow)
    {
        if (!map.TryGetValue("variables", out var variablesObj) || variablesObj is not Dictionary<string, object?> variablesMap)
        {
            return;
        }

        foreach (var (name, value) in variablesMap)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            workflow.Variables[name] = NormalizeValue(value);
        }
    }

    private static void ParseStages(IDictionary<string, object?> map, WorkflowDefinition workflow)
    {
        foreach (var stageObj in GetList(map, "stages"))
        {
            if (stageObj is not Dictionary<string, object?> stageMap)
            {
                continue;
            }

            var stage = new StageDefinition
            {
                Stage = GetString(stageMap, "stage")
            };

            foreach (var jobObj in GetList(stageMap, "jobs"))
            {
                if (jobObj is not Dictionary<string, object?> jobMap)
                {
                    continue;
                }

                var job = new JobDefinition
                {
                    Job = GetString(jobMap, "job"),
                    MaxParallelism = GetNullableInt(jobMap, "max_parallelism"),
                    ContinueOnError = GetNullableBool(jobMap, "continue_on_error")
                };

                foreach (var stepObj in GetList(jobMap, "steps"))
                {
                    if (stepObj is not Dictionary<string, object?> stepMap)
                    {
                        continue;
                    }

                    var step = new StepDefinition
                    {
                        Step = GetString(stepMap, "step"),
                        Type = GetString(stepMap, "type"),
                        Condition = GetOptionalString(stepMap, "condition"),
                        TimeoutMs = GetNullableInt(stepMap, "timeout_ms"),
                        Retries = GetNullableInt(stepMap, "retries"),
                        ContinueOnError = GetNullableBool(stepMap, "continue_on_error")
                    };

                    if (stepMap.TryGetValue("with", out var withObj) && withObj is Dictionary<string, object?> withMap)
                    {
                        foreach (var (k, v) in withMap)
                        {
                            step.With[k] = NormalizeValue(v);
                        }
                    }

                    if (stepMap.TryGetValue("depends_on", out var dependsObj))
                    {
                        foreach (var dep in ToStringList(dependsObj))
                        {
                            step.DependsOn.Add(dep);
                        }
                    }

                    job.Steps.Add(step);
                }

                stage.Jobs.Add(job);
            }

            workflow.Stages.Add(stage);
        }
    }

    private static bool LooksLikeParameterDefinition(IReadOnlyDictionary<string, object?> map)
    {
        return map.ContainsKey("type")
            || map.ContainsKey("required")
            || map.ContainsKey("default")
            || map.ContainsKey("description")
            || map.ContainsKey("allowed_values")
            || map.ContainsKey("min")
            || map.ContainsKey("max")
            || map.ContainsKey("min_length")
            || map.ContainsKey("max_length")
            || map.ContainsKey("pattern")
            || map.ContainsKey("item_type")
            || map.ContainsKey("required_properties");
    }

    private static object NormalizeValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is Dictionary<string, object?> map)
        {
            var normalized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, childValue) in map)
            {
                normalized[key] = NormalizeValue(childValue);
            }

            return normalized;
        }

        if (value is List<object?> list)
        {
            var normalized = new List<object>(list.Count);
            foreach (var item in list)
            {
                normalized.Add(NormalizeValue(item));
            }

            return normalized;
        }

        return value;
    }

    private static string GetString(IDictionary<string, object?> map, string key, string defaultValue = "")
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return value.ToString() ?? defaultValue;
    }

    private static string? GetOptionalString(IDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        var text = value.ToString();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static int GetInt(IDictionary<string, object?> map, string key, int defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return int.TryParse(value.ToString(), out var i) ? i : defaultValue;
    }

    private static int? GetNullableInt(IDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static double? GetNullableDouble(IDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return double.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static bool GetBool(IDictionary<string, object?> map, string key, bool defaultValue)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return defaultValue;
        }

        return bool.TryParse(value.ToString(), out var parsed) ? parsed : defaultValue;
    }

    private static bool? GetNullableBool(IDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return bool.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }

    private static List<object?> GetList(IDictionary<string, object?> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || value is null)
        {
            return new List<object?>();
        }

        return value as List<object?> ?? new List<object?>();
    }

    private static List<object> GetObjectList(IDictionary<string, object?> map, string key)
    {
        var values = GetList(map, key);
        var normalized = new List<object>(values.Count);
        foreach (var value in values)
        {
            normalized.Add(NormalizeValue(value));
        }

        return normalized;
    }

    private static IEnumerable<string> ToStringList(object? value)
    {
        if (value is null)
        {
            yield break;
        }

        if (value is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
            {
                yield return s;
            }

            yield break;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                var text = item.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return text!;
                }
            }
        }
    }
}
