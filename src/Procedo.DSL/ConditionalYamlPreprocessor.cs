using Procedo.Expressions;

namespace Procedo.DSL;

internal static class ConditionalYamlPreprocessor
{
    public static Dictionary<string, object?> ProcessRoot(Dictionary<string, object?> root)
    {
        var context = BuildContext(root);
        return ProcessMapping(root, context);
    }

    private static Dictionary<string, object?> ProcessMapping(
        IReadOnlyDictionary<string, object?> map,
        IDictionary<string, object> context)
    {
        var output = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var entries = map.ToList();

        for (var index = 0; index < entries.Count; index++)
        {
            var (key, value) = entries[index];
            if (TryParseEachDirective(key, out var itemName, out var collectionExpression))
            {
                ExpandEachMapping(output, value, itemName, collectionExpression, context);
                continue;
            }

            if (!TryParseDirective(key, out var kind, out var expression))
            {
                output[key] = ProcessNode(value, context);
                continue;
            }

            if (!string.Equals(kind, "if", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unexpected conditional branch '{key}' without a preceding if block.");
            }

            var selectedBranch = SelectBranch(entries, ref index, context);
            if (selectedBranch is null)
            {
                continue;
            }

            var processed = ProcessNode(selectedBranch, context);
            if (processed is Dictionary<string, object?> selectedMap)
            {
                foreach (var (selectedKey, selectedValue) in selectedMap)
                {
                    output[selectedKey] = selectedValue;
                }

                continue;
            }

            throw new InvalidOperationException("Conditional mapping branches must evaluate to mappings.");
        }

        return output;
    }

    private static List<object?> ProcessSequence(
        IReadOnlyList<object?> sequence,
        IDictionary<string, object> context)
    {
        var output = new List<object?>();

        for (var index = 0; index < sequence.Count; index++)
        {
            if (TryGetSequenceEachDirective(sequence[index], out var itemName, out var collectionExpression, out var eachValue))
            {
                ExpandEachSequence(output, eachValue, itemName, collectionExpression, context);
                continue;
            }

            if (!TryGetSequenceDirective(sequence[index], out var kind, out var expression, out var branchValue))
            {
                output.Add(ProcessNode(sequence[index], context));
                continue;
            }

            if (!string.Equals(kind, "if", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Unexpected elseif/else branch in sequence without a preceding if block.");
            }

            var selected = SelectSequenceBranch(sequence, ref index, context);
            if (selected is null)
            {
                continue;
            }

            var processed = ProcessNode(selected, context);
            if (processed is List<object?> list)
            {
                output.AddRange(list);
            }
            else
            {
                output.Add(processed);
            }
        }

        return output;
    }

    private static object? ProcessNode(object? value, IDictionary<string, object> context)
    {
        return value switch
        {
            Dictionary<string, object?> map when IsDirectiveOnlyMapping(map) => ProcessDirectiveOnlyMapping(map, context),
            Dictionary<string, object?> map => ProcessMapping(map, context),
            List<object?> list => ProcessSequence(list, context),
            string text => text,
            _ => value
        };
    }

    private static object? ProcessDirectiveOnlyMapping(
        IReadOnlyDictionary<string, object?> map,
        IDictionary<string, object> context)
    {
        var entries = map.ToList();
        var index = 0;
        if (TryParseEachDirective(entries[0].Key, out var itemName, out var collectionExpression))
        {
            var expanded = new List<object?>();
            ExpandEachSequence(expanded, entries[0].Value, itemName, collectionExpression, context);
            return expanded;
        }

        if (!TryParseDirective(entries[0].Key, out var kind, out _)
            || !string.Equals(kind, "if", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Conditional blocks must start with an if branch or an each block.");
        }

        var selected = SelectBranch(entries, ref index, context);
        return selected is null ? null : ProcessNode(selected, context);
    }

    private static void ExpandEachMapping(
        IDictionary<string, object?> output,
        object? value,
        string itemName,
        string collectionExpression,
        IDictionary<string, object> context)
    {
        foreach (var itemContext in EnumerateEachContexts(itemName, collectionExpression, context))
        {
            var substituted = SubstituteLoopValue(value, itemName, itemContext[itemName]);
            var processed = ProcessNode(substituted, itemContext);
            if (processed is not Dictionary<string, object?> processedMap)
            {
                throw new InvalidOperationException("Each mapping blocks must evaluate to mappings.");
            }

            foreach (var (key, childValue) in processedMap)
            {
                output[key] = childValue;
            }
        }
    }

    private static void ExpandEachSequence(
        IList<object?> output,
        object? value,
        string itemName,
        string collectionExpression,
        IDictionary<string, object> context)
    {
        foreach (var itemContext in EnumerateEachContexts(itemName, collectionExpression, context))
        {
            var substituted = SubstituteLoopValue(value, itemName, itemContext[itemName]);
            var processed = ProcessNode(substituted, itemContext);
            if (processed is List<object?> list)
            {
                foreach (var listItem in list)
                {
                    output.Add(listItem);
                }

                continue;
            }

            output.Add(processed);
        }
    }

    private static object? SelectBranch(
        IReadOnlyList<KeyValuePair<string, object?>> entries,
        ref int index,
        IDictionary<string, object> context)
    {
        object? selected = null;

        while (index < entries.Count)
        {
            var (key, value) = entries[index];
            if (!TryParseDirective(key, out var kind, out var expression))
            {
                index--;
                break;
            }

            var isMatch = kind switch
            {
                "if" or "elseif" => EvaluateCondition(expression!, context),
                "else" => true,
                _ => false
            };

            if (selected is null && isMatch)
            {
                selected = value;
            }

            index++;
            if (index >= entries.Count || !TryParseDirective(entries[index].Key, out _, out _))
            {
                index--;
                break;
            }
        }

        return selected;
    }

    private static object? SelectSequenceBranch(
        IReadOnlyList<object?> items,
        ref int index,
        IDictionary<string, object> context)
    {
        object? selected = null;

        while (index < items.Count)
        {
            if (!TryGetSequenceDirective(items[index], out var kind, out var expression, out var branchValue))
            {
                index--;
                break;
            }

            var isMatch = kind switch
            {
                "if" or "elseif" => EvaluateCondition(expression!, context),
                "else" => true,
                _ => false
            };

            if (selected is null && isMatch)
            {
                selected = branchValue;
            }

            index++;
            if (index >= items.Count || !TryGetSequenceDirective(items[index], out _, out _, out _))
            {
                index--;
                break;
            }
        }

        return selected;
    }

    private static IEnumerable<IDictionary<string, object>> EnumerateEachContexts(
        string itemName,
        string collectionExpression,
        IDictionary<string, object> context)
    {
        var evaluated = ExpressionResolver.EvaluateExpression(collectionExpression, context);
        if (evaluated is string || evaluated is System.Collections.IDictionary)
        {
            throw new InvalidOperationException($"Each expression '{collectionExpression}' must evaluate to an array, not a string or object.");
        }

        if (evaluated is not System.Collections.IEnumerable enumerable)
        {
            throw new InvalidOperationException($"Each expression '{collectionExpression}' must evaluate to an array.");
        }

        foreach (var item in enumerable)
        {
            var childContext = new Dictionary<string, object>(context, StringComparer.OrdinalIgnoreCase)
            {
                [itemName] = item!
            };
            yield return childContext;
        }
    }

    private static bool EvaluateCondition(string expression, IDictionary<string, object> context)
        => ExpressionResolver.EvaluateCondition(expression, context);

    private static IDictionary<string, object> BuildContext(IReadOnlyDictionary<string, object?> root)
    {
        var context = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (root.TryGetValue("parameters", out var parametersObj) && parametersObj is Dictionary<string, object?> parametersMap)
        {
            foreach (var (name, value) in parametersMap)
            {
                if (value is Dictionary<string, object?> parameterDefinition && LooksLikeParameterDefinition(parameterDefinition))
                {
                    if (parameterDefinition.TryGetValue("default", out var defaultValue) && defaultValue is not null)
                    {
                        context[$"params.{name}"] = NormalizeValue(defaultValue);
                    }

                    continue;
                }

                context[$"params.{name}"] = NormalizeValue(value);
            }
        }

        if (root.TryGetValue("variables", out var variablesObj) && variablesObj is Dictionary<string, object?> variablesMap)
        {
            var rawVariables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, value) in variablesMap)
            {
                if (TryParseDirective(name, out _, out _))
                {
                    continue;
                }

                rawVariables[name] = NormalizeValue(value);
            }

            try
            {
                var resolvedVariables = WorkflowContextResolver.ResolveWorkflowVariables(rawVariables, context);
                foreach (var (name, value) in resolvedVariables)
                {
                    context[name] = value;
                    context[$"vars.{name}"] = value;
                }
            }
            catch (WorkflowContextResolutionException)
            {
            }
        }

        return context;
    }

    private static bool TryGetSequenceDirective(object? item, out string kind, out string? expression, out object? branchValue)
    {
        kind = string.Empty;
        expression = null;
        branchValue = null;

        if (item is not Dictionary<string, object?> map || map.Count != 1)
        {
            return false;
        }

        var pair = map.Single();
        if (!TryParseDirective(pair.Key, out kind, out expression))
        {
            return false;
        }

        branchValue = pair.Value;
        return true;
    }

    private static bool TryGetSequenceEachDirective(
        object? item,
        out string itemName,
        out string collectionExpression,
        out object? branchValue)
    {
        itemName = string.Empty;
        collectionExpression = string.Empty;
        branchValue = null;

        if (item is not Dictionary<string, object?> map || map.Count != 1)
        {
            return false;
        }

        var pair = map.Single();
        if (!TryParseEachDirective(pair.Key, out itemName, out collectionExpression))
        {
            return false;
        }

        branchValue = pair.Value;
        return true;
    }

    private static bool TryParseDirective(string key, out string kind, out string? expression)
    {
        kind = string.Empty;
        expression = null;

        var trimmed = key.Trim();
        if (!trimmed.StartsWith("${{", StringComparison.Ordinal) || !trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return false;
        }

        var body = trimmed[3..^2].Trim();
        if (body.StartsWith("if ", StringComparison.OrdinalIgnoreCase))
        {
            kind = "if";
            expression = body[3..].Trim();
            return true;
        }

        if (body.StartsWith("elseif ", StringComparison.OrdinalIgnoreCase))
        {
            kind = "elseif";
            expression = body[7..].Trim();
            return true;
        }

        if (string.Equals(body, "else", StringComparison.OrdinalIgnoreCase))
        {
            kind = "else";
            return true;
        }

        return false;
    }

    private static bool TryParseEachDirective(string key, out string itemName, out string collectionExpression)
    {
        itemName = string.Empty;
        collectionExpression = string.Empty;

        var trimmed = key.Trim();
        if (!trimmed.StartsWith("${{", StringComparison.Ordinal) || !trimmed.EndsWith("}}", StringComparison.Ordinal))
        {
            return false;
        }

        var body = trimmed[3..^2].Trim();
        if (!body.StartsWith("each ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remainder = body[5..].Trim();
        var inIndex = remainder.IndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (inIndex <= 0)
        {
            throw new InvalidOperationException($"Invalid each expression '{key}'. Expected '${{{{ each item in collection }}}}'.");
        }

        itemName = remainder[..inIndex].Trim();
        collectionExpression = remainder[(inIndex + 4)..].Trim();
        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(collectionExpression))
        {
            throw new InvalidOperationException($"Invalid each expression '{key}'. Expected '${{{{ each item in collection }}}}'.");
        }

        return true;
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

    private static bool IsDirectiveOnlyMapping(IReadOnlyDictionary<string, object?> map)
        => map.Count > 0 && map.Keys.All(key => TryParseDirective(key, out _, out _) || TryParseEachDirective(key, out _, out _));

    private static object NormalizeValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            Dictionary<string, object?> map => map.ToDictionary(static pair => pair.Key, static pair => NormalizeValue(pair.Value), StringComparer.OrdinalIgnoreCase),
            List<object?> list => list.Select(NormalizeValue).ToList(),
            _ => value
        };
    }

    private static object? SubstituteLoopValue(object? value, string itemName, object itemValue)
    {
        return value switch
        {
            null => null,
            string text => text.Replace($"${{{itemName}}}", itemValue?.ToString() ?? string.Empty, StringComparison.Ordinal),
            Dictionary<string, object?> map => map.ToDictionary(
                static pair => pair.Key,
                pair => SubstituteLoopValue(pair.Value, itemName, itemValue),
                StringComparer.OrdinalIgnoreCase),
            List<object?> list => list.Select(item => SubstituteLoopValue(item, itemName, itemValue)).ToList(),
            _ => value
        };
    }
}
