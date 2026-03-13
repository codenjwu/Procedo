using Procedo.Core.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Procedo.Expressions;

public static class WorkflowContextResolver
{
    public static Dictionary<string, object> BuildInitialVariables(
        WorkflowDefinition workflow,
        IDictionary<string, object>? runtimeParameters = null)
    {
        if (workflow is null)
        {
            throw new ArgumentNullException(nameof(workflow));
        }

        var resolvedParameters = ResolveParameters(workflow, runtimeParameters);
        var variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in resolvedParameters)
        {
            variables[$"params.{key}"] = value;
        }

        var resolvedVariables = ResolveWorkflowVariables(workflow.Variables, variables);
        foreach (var (key, value) in resolvedVariables)
        {
            variables[key] = value;
            variables[$"vars.{key}"] = value;
        }

        return variables;
    }

    public static Dictionary<string, object> ResolveParameters(
        WorkflowDefinition workflow,
        IDictionary<string, object>? runtimeParameters = null)
    {
        var provided = new Dictionary<string, object>(workflow.ParameterValues, StringComparer.OrdinalIgnoreCase);
        if (runtimeParameters is not null)
        {
            foreach (var (name, value) in runtimeParameters)
            {
                provided[name] = value;
            }
        }

        var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, definition) in workflow.ParameterDefinitions)
        {
            if (provided.TryGetValue(name, out var providedValue))
            {
                resolved[name] = CoerceParameterValue(name, definition, providedValue);
                continue;
            }

            if (definition.Default is not null)
            {
                resolved[name] = CoerceParameterValue(name, definition, definition.Default);
            }
        }

        foreach (var (name, value) in provided)
        {
            if (!workflow.ParameterDefinitions.ContainsKey(name))
            {
                resolved[name] = value;
            }
        }

        return resolved;
    }

    public static Dictionary<string, object> ResolveWorkflowVariables(
        IDictionary<string, object> rawVariables,
        IDictionary<string, object> parameterScope)
    {
        var resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        var remaining = new Dictionary<string, object>(rawVariables, StringComparer.OrdinalIgnoreCase);

        while (remaining.Count > 0)
        {
            var progress = false;
            foreach (var (name, rawValue) in remaining.ToArray())
            {
                var scope = new Dictionary<string, object>(parameterScope, StringComparer.OrdinalIgnoreCase);
                foreach (var (resolvedName, resolvedValue) in resolved)
                {
                    scope[resolvedName] = resolvedValue;
                    scope[$"vars.{resolvedName}"] = resolvedValue;
                }

                try
                {
                    resolved[name] = ExpressionResolver.ResolveValue(rawValue, scope);
                    remaining.Remove(name);
                    progress = true;
                }
                catch (ExpressionResolutionException)
                {
                }
            }

            if (progress)
            {
                continue;
            }

            var unresolved = string.Join(", ", remaining.Keys.OrderBy(static x => x, StringComparer.OrdinalIgnoreCase));
            throw new WorkflowContextResolutionException($"Unable to resolve workflow variables: {unresolved}.");
        }

        return resolved;
    }

    public static bool IsCompatibleParameterValue(ParameterDefinition definition, object? value)
    {
        try
        {
            _ = CoerceParameterValue("parameter", definition, value);
            return true;
        }
        catch (WorkflowContextResolutionException)
        {
            return false;
        }
    }

    private static object CoerceParameterValue(string name, ParameterDefinition definition, object? value)
    {
        var coerced = CoerceParameterValueByType(name, definition.Type, value, definition.ItemType);
        ValidateParameterConstraints(name, definition, coerced);
        return coerced;
    }

    private static object CoerceParameterValueByType(string name, string? declaredType, object? value, string? itemType = null)
    {
        var type = NormalizeType(declaredType);

        return type switch
        {
            "string" => value?.ToString() ?? string.Empty,
            "int" or "integer" => CoerceInteger(name, value),
            "bool" or "boolean" => CoerceBoolean(name, value),
            "number" or "double" or "float" or "decimal" => CoerceNumber(name, value),
            "object" => CoerceObject(name, value),
            "array" or "list" => CoerceArray(name, value, itemType),
            _ => value ?? string.Empty
        };
    }

    private static string NormalizeType(string? declaredType)
        => string.IsNullOrWhiteSpace(declaredType) ? "string" : declaredType.Trim().ToLowerInvariant();

    private static object CoerceInteger(string name, object? value)
    {
        if (value is int intValue)
        {
            return intValue;
        }

        if (value is long longValue && longValue is >= int.MinValue and <= int.MaxValue)
        {
            return (int)longValue;
        }

        if (int.TryParse(value?.ToString(), out var parsed))
        {
            return parsed;
        }

        throw new WorkflowContextResolutionException($"Parameter '{name}' could not be converted to an integer.");
    }

    private static object CoerceBoolean(string name, object? value)
    {
        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (bool.TryParse(value?.ToString(), out var parsed))
        {
            return parsed;
        }

        throw new WorkflowContextResolutionException($"Parameter '{name}' could not be converted to a boolean.");
    }

    private static object CoerceNumber(string name, object? value)
    {
        if (value is double doubleValue)
        {
            return doubleValue;
        }

        if (value is float floatValue)
        {
            return (double)floatValue;
        }

        if (value is decimal decimalValue)
        {
            return (double)decimalValue;
        }

        if (value is int intValue)
        {
            return (double)intValue;
        }

        if (value is long longValue)
        {
            return (double)longValue;
        }

        if (double.TryParse(value?.ToString(), out var parsed))
        {
            return parsed;
        }

        throw new WorkflowContextResolutionException($"Parameter '{name}' could not be converted to a number.");
    }

    private static object CoerceObject(string name, object? value)
    {
        if (value is IDictionary<string, object> genericDictionary)
        {
            return new Dictionary<string, object>(genericDictionary, StringComparer.OrdinalIgnoreCase);
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                converted[entry.Key?.ToString() ?? string.Empty] = entry.Value ?? string.Empty;
            }

            return converted;
        }

        throw new WorkflowContextResolutionException($"Parameter '{name}' must be an object value.");
    }

    private static object CoerceArray(string name, object? value, string? itemType = null)
    {
        if (value is IEnumerable<object> typedEnumerable && value is not string)
        {
            var typedList = typedEnumerable.ToList();
            return string.IsNullOrWhiteSpace(itemType)
                ? typedList
                : typedList.Select(item => CoerceParameterValueByType($"{name}[]", itemType, item)).ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var converted = new List<object>();
            foreach (var item in enumerable)
            {
                converted.Add(string.IsNullOrWhiteSpace(itemType)
                    ? item ?? string.Empty
                    : CoerceParameterValueByType($"{name}[]", itemType, item));
            }

            return converted;
        }

        throw new WorkflowContextResolutionException($"Parameter '{name}' must be an array value.");
    }

    private static void ValidateParameterConstraints(string name, ParameterDefinition definition, object value)
    {
        if (definition.AllowedValues.Count > 0)
        {
            var matches = definition.AllowedValues.Any(allowed =>
            {
                var coercedAllowed = CoerceParameterValueByType(name, definition.Type, allowed, definition.ItemType);
                return AreEquivalentValues(value, coercedAllowed);
            });

            if (!matches)
            {
                throw new WorkflowContextResolutionException($"Parameter '{name}' must be one of the allowed values.");
            }
        }

        if (value is string text)
        {
            if (definition.MinLength is not null && text.Length < definition.MinLength.Value)
            {
                throw new WorkflowContextResolutionException($"Parameter '{name}' must be at least {definition.MinLength.Value} characters long.");
            }

            if (definition.MaxLength is not null && text.Length > definition.MaxLength.Value)
            {
                throw new WorkflowContextResolutionException($"Parameter '{name}' must be at most {definition.MaxLength.Value} characters long.");
            }

            if (!string.IsNullOrWhiteSpace(definition.Pattern) && !Regex.IsMatch(text, definition.Pattern))
            {
                throw new WorkflowContextResolutionException($"Parameter '{name}' does not match the required pattern.");
            }
        }

        if (value is int intValue)
        {
            ValidateNumericRange(name, definition, intValue);
        }
        else if (value is double doubleValue)
        {
            ValidateNumericRange(name, definition, doubleValue);
        }

        if (value is IDictionary<string, object> objectValue)
        {
            foreach (var requiredProperty in definition.RequiredProperties)
            {
                if (!objectValue.ContainsKey(requiredProperty))
                {
                    throw new WorkflowContextResolutionException($"Parameter '{name}' must include required property '{requiredProperty}'.");
                }
            }
        }
    }

    private static void ValidateNumericRange(string name, ParameterDefinition definition, double value)
    {
        if (definition.Minimum is not null && value < definition.Minimum.Value)
        {
            throw new WorkflowContextResolutionException($"Parameter '{name}' must be greater than or equal to {definition.Minimum.Value}.");
        }

        if (definition.Maximum is not null && value > definition.Maximum.Value)
        {
            throw new WorkflowContextResolutionException($"Parameter '{name}' must be less than or equal to {definition.Maximum.Value}.");
        }
    }

    private static bool AreEquivalentValues(object left, object right)
    {
        if (Equals(left, right))
        {
            return true;
        }

        return JsonSerializer.Serialize(left) == JsonSerializer.Serialize(right);
    }
}
