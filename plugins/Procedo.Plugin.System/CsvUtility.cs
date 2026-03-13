using System.Text;

namespace Procedo.Plugin.System;

internal static class CsvUtility
{
    public static List<Dictionary<string, object>> Read(string content, char delimiter, bool hasHeader)
    {
        var rows = ParseRows(content, delimiter);
        if (rows.Count == 0)
        {
            return new List<Dictionary<string, object>>();
        }

        var headers = hasHeader
            ? rows[0]
            : Enumerable.Range(0, rows[0].Count).Select(i => $"column{i}").ToList();

        var startIndex = hasHeader ? 1 : 0;
        var result = new List<Dictionary<string, object>>();
        for (var i = startIndex; i < rows.Count; i++)
        {
            var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var j = 0; j < headers.Count; j++)
            {
                row[headers[j]] = j < rows[i].Count ? rows[i][j] : string.Empty;
            }

            result.Add(row);
        }

        return result;
    }

    public static string Write(IEnumerable<IDictionary<string, object>> rows, char delimiter, bool includeHeader)
    {
        var materialized = rows.Select(row => new Dictionary<string, object>(row, StringComparer.OrdinalIgnoreCase)).ToList();
        if (materialized.Count == 0)
        {
            return string.Empty;
        }

        var headers = materialized.SelectMany(row => row.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var builder = new StringBuilder();

        if (includeHeader)
        {
            builder.AppendLine(string.Join(delimiter, headers.Select(value => Escape(value, delimiter))));
        }

        foreach (var row in materialized)
        {
            builder.AppendLine(string.Join(delimiter, headers.Select(header => Escape(row.TryGetValue(header, out var value) ? value?.ToString() ?? string.Empty : string.Empty, delimiter))));
        }

        return builder.ToString();
    }

    private static List<List<string>> ParseRows(string content, char delimiter)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var currentValue = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentValue.Append(c);
                }

                continue;
            }

            if (c == '"')
            {
                inQuotes = true;
                continue;
            }

            if (c == delimiter)
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                continue;
            }

            if (c == '\r')
            {
                continue;
            }

            if (c == '\n')
            {
                currentRow.Add(currentValue.ToString());
                currentValue.Clear();
                rows.Add(currentRow);
                currentRow = new List<string>();
                continue;
            }

            currentValue.Append(c);
        }

        if (currentValue.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentValue.ToString());
            rows.Add(currentRow);
        }

        return rows;
    }

    private static string Escape(string value, char delimiter)
    {
        if (value.Contains(delimiter) || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
