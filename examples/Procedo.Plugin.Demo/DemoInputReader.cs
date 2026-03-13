using System;
using System.Globalization;

namespace Procedo.Plugin.Demo;

internal static class DemoInputReader
{
    public static string GetString(object? value, string fallback = "")
        => value?.ToString() ?? fallback;

    public static int GetInt(object? value, int fallback)
    {
        if (value is null)
        {
            return fallback;
        }

        if (value is int i)
        {
            return i;
        }

        if (value is long l && l <= int.MaxValue && l >= int.MinValue)
        {
            return (int)l;
        }

        if (value is double d)
        {
            return (int)d;
        }

        if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return fallback;
    }
}
