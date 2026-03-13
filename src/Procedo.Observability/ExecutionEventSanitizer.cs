namespace Procedo.Observability;

internal static class ExecutionEventSanitizer
{
    private const string RedactedValue = "***REDACTED***";

    private static readonly string[] SensitiveKeyFragments =
    {
        "secret",
        "token",
        "password",
        "passwd",
        "pwd",
        "apikey",
        "api_key",
        "accesskey",
        "access_key",
        "clientsecret",
        "client_secret",
        "connectionstring",
        "connection_string",
        "authorization",
        "cookie",
        "privatekey",
        "private_key"
    };

    public static ExecutionEvent Sanitize(ExecutionEvent executionEvent)
    {
        if (executionEvent.Outputs is null || executionEvent.Outputs.Count == 0)
        {
            return executionEvent;
        }

        executionEvent.Outputs = SanitizeDictionary(executionEvent.Outputs, forceRedactAllDescendants: false);
        return executionEvent;
    }

    private static Dictionary<string, object> SanitizeDictionary(
        IDictionary<string, object> source,
        bool forceRedactAllDescendants)
    {
        var sanitized = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in source)
        {
            var redactBranch = forceRedactAllDescendants || IsPayloadKey(pair.Key) || IsSensitiveKey(pair.Key);
            sanitized[pair.Key] = SanitizeValue(pair.Value, redactBranch);
        }

        return sanitized;
    }

    private static object SanitizeValue(object? value, bool forceRedact)
    {
        if (forceRedact)
        {
            return RedactStructure(value);
        }

        if (value is null)
        {
            return string.Empty;
        }

        if (value is IDictionary<string, object> genericDictionary)
        {
            return SanitizeDictionary(genericDictionary, forceRedactAllDescendants: false);
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var mapped = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString() ?? string.Empty;
                mapped[key] = SanitizeValue(entry.Value, IsPayloadKey(key) || IsSensitiveKey(key));
            }

            return mapped;
        }

        if (value is IEnumerable<object> typedEnumerable && value is not string)
        {
            return typedEnumerable.Select(item => SanitizeValue(item, forceRedact: false)).ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(SanitizeValue(item, forceRedact: false));
            }

            return list;
        }

        return value;
    }

    private static object RedactStructure(object? value)
    {
        if (value is null)
        {
            return RedactedValue;
        }

        if (value is IDictionary<string, object> genericDictionary)
        {
            return genericDictionary.ToDictionary(
                pair => pair.Key,
                pair => (object)RedactStructure(pair.Value),
                StringComparer.OrdinalIgnoreCase);
        }

        if (value is System.Collections.IDictionary dictionary)
        {
            var mapped = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (System.Collections.DictionaryEntry entry in dictionary)
            {
                mapped[entry.Key?.ToString() ?? string.Empty] = RedactStructure(entry.Value);
            }

            return mapped;
        }

        if (value is IEnumerable<object> typedEnumerable && value is not string)
        {
            return typedEnumerable.Select(RedactStructure).Cast<object>().ToList();
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(RedactStructure(item));
            }

            return list;
        }

        return RedactedValue;
    }

    private static bool IsPayloadKey(string key)
        => string.Equals(key, "payload", StringComparison.OrdinalIgnoreCase);

    private static bool IsSensitiveKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return SensitiveKeyFragments.Any(fragment =>
            key.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
