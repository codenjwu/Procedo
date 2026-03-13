using System.Collections;
using System.Globalization;

namespace Procedo.Plugin.System;

internal static class SystemInputReader
{
    public static string GetString(object? value, string fallback = "")
        => value?.ToString() ?? fallback;

    public static int GetInt(object? value, int fallback = 0)
    {
        if (value is null)
        {
            return fallback;
        }

        return value switch
        {
            int i => i,
            long l when l >= int.MinValue && l <= int.MaxValue => (int)l,
            double d when d >= int.MinValue && d <= int.MaxValue => (int)d,
            float f when f >= int.MinValue && f <= int.MaxValue => (int)f,
            _ => int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback
        };
    }

    public static long GetLong(object? value, long fallback = 0)
    {
        if (value is null)
        {
            return fallback;
        }

        return value switch
        {
            long l => l,
            int i => i,
            double d when d >= long.MinValue && d <= long.MaxValue => (long)d,
            float f when f >= long.MinValue && f <= long.MaxValue => (long)f,
            _ => long.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback
        };
    }

    public static bool GetBool(object? value, bool fallback = false)
    {
        if (value is null)
        {
            return fallback;
        }

        return value switch
        {
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            _ => fallback
        };
    }

    public static IDictionary<string, object> GetDictionary(object? value)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is null)
                {
                    continue;
                }

                result[entry.Key.ToString() ?? string.Empty] = entry.Value ?? string.Empty;
            }
        }

        return result;
    }

    public static IEnumerable<object> GetValues(object? value)
    {
        if (value is null)
        {
            return Array.Empty<object>();
        }

        if (value is string s)
        {
            return new object[] { s };
        }

        if (value is IEnumerable enumerable)
        {
            var values = new List<object>();
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    values.Add(item);
                }
            }

            return values;
        }

        return new object[] { value };
    }
}
