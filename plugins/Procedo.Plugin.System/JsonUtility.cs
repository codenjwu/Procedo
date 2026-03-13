using System.Text.Json;

namespace Procedo.Plugin.System;

internal static class JsonUtility
{
    public static object? DeserializeToObject(string json)
    {
        using var document = JsonDocument.Parse(json);
        return ConvertElement(document.RootElement);
    }

    public static string Serialize(object? value)
        => JsonSerializer.Serialize(value);

    public static object? GetValue(object? root, string path)
    {
        var segments = ParsePath(path);
        object? current = root;

        foreach (var segment in segments)
        {
            if (segment.IsIndex)
            {
                if (current is not IList<object> list || segment.Index < 0 || segment.Index >= list.Count)
                {
                    return null;
                }

                current = list[segment.Index];
            }
            else
            {
                if (current is not IDictionary<string, object?> dict || !dict.TryGetValue(segment.Property!, out current))
                {
                    return null;
                }
            }
        }

        return current;
    }

    public static bool SetValue(object? root, string path, object? value)
    {
        var segments = ParsePath(path);
        if (segments.Count == 0)
        {
            return false;
        }

        object? current = root;
        for (var i = 0; i < segments.Count - 1; i++)
        {
            var segment = segments[i];
            if (segment.IsIndex)
            {
                if (current is not IList<object> list || segment.Index < 0 || segment.Index >= list.Count)
                {
                    return false;
                }

                current = list[segment.Index];
                continue;
            }

            if (current is not IDictionary<string, object?> dict)
            {
                return false;
            }

            if (!dict.TryGetValue(segment.Property!, out var next) || next is null)
            {
                next = segments[i + 1].IsIndex
                    ? new List<object>()
                    : new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                dict[segment.Property!] = next;
            }

            current = next;
        }

        var finalSegment = segments[^1];
        if (finalSegment.IsIndex)
        {
            if (current is not IList<object> list || finalSegment.Index < 0)
            {
                return false;
            }

            while (list.Count <= finalSegment.Index)
            {
                list.Add(null!);
            }

            list[finalSegment.Index] = Normalize(value)!;
            return true;
        }

        if (current is not IDictionary<string, object?> finalDict)
        {
            return false;
        }

        finalDict[finalSegment.Property!] = Normalize(value);
        return true;
    }

    public static object? Merge(object? left, object? right)
    {
        if (left is IDictionary<string, object?> leftDict && right is IDictionary<string, object?> rightDict)
        {
            var merged = new Dictionary<string, object?>(leftDict, StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in rightDict)
            {
                merged[key] = merged.TryGetValue(key, out var existing)
                    ? Merge(existing, value)
                    : Normalize(value);
            }

            return merged;
        }

        if (left is IList<object> leftList && right is IList<object> rightList)
        {
            return leftList.Concat(rightList).Select(Normalize).ToList();
        }

        return Normalize(right);
    }

    private static object? Normalize(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement element => ConvertElement(element),
            IDictionary<string, object?> dict => dict.ToDictionary(kvp => kvp.Key, kvp => Normalize(kvp.Value), StringComparer.OrdinalIgnoreCase),
            IList<object> list => list.Select(Normalize).ToList(),
            _ => value
        };
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ConvertElement(property.Value),
                StringComparer.OrdinalIgnoreCase),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    private static IReadOnlyList<PathSegment> ParsePath(string path)
    {
        var result = new List<PathSegment>();
        var buffer = string.Empty;

        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '.')
            {
                if (!string.IsNullOrWhiteSpace(buffer))
                {
                    result.Add(PathSegment.ForProperty(buffer));
                    buffer = string.Empty;
                }

                continue;
            }

            if (c == '[')
            {
                if (!string.IsNullOrWhiteSpace(buffer))
                {
                    result.Add(PathSegment.ForProperty(buffer));
                    buffer = string.Empty;
                }

                var end = path.IndexOf(']', i + 1);
                if (end < 0)
                {
                    break;
                }

                var indexText = path.Substring(i + 1, end - i - 1);
                if (int.TryParse(indexText, out var index))
                {
                    result.Add(PathSegment.ForIndex(index));
                }

                i = end;
                continue;
            }

            buffer += c;
        }

        if (!string.IsNullOrWhiteSpace(buffer))
        {
            result.Add(PathSegment.ForProperty(buffer));
        }

        return result;
    }

    private readonly record struct PathSegment(string? Property, int Index, bool IsIndex)
    {
        public static PathSegment ForProperty(string property) => new(property, -1, false);
        public static PathSegment ForIndex(int index) => new(null, index, true);
    }
}
