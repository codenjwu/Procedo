using System;
using System.Collections.Generic;

namespace Procedo.DSL.Parsing;

internal static class MinimalYamlParser
{
    public static object Parse(string yaml)
    {
        var lines = PrepareLines(yaml);
        var index = 0;
        return ParseNode(lines, ref index, 0);
    }

    private static object ParseNode(IReadOnlyList<YamlLine> lines, ref int index, int indent)
    {
        if (index >= lines.Count)
        {
            return new Dictionary<string, object?>();
        }

        return lines[index].Content.StartsWith("- ", StringComparison.Ordinal)
            ? ParseSequence(lines, ref index, indent)
            : ParseMapping(lines, ref index, indent);
    }

    private static Dictionary<string, object?> ParseMapping(IReadOnlyList<YamlLine> lines, ref int index, int indent)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Indent < indent)
            {
                break;
            }

            if (line.Indent > indent)
            {
                throw new InvalidOperationException($"Unexpected indentation at line {line.LineNumber}.");
            }

            var separatorIndex = line.Content.IndexOf(':');
            if (separatorIndex < 0)
            {
                throw new InvalidOperationException($"Invalid mapping at line {line.LineNumber}: {line.Content}");
            }

            var key = line.Content[..separatorIndex].Trim();
            var remainder = line.Content[(separatorIndex + 1)..].Trim();
            index++;

            if (remainder.Length > 0)
            {
                map[key] = ParseScalar(remainder);
                continue;
            }

            if (index < lines.Count &&
                (lines[index].Indent > indent || (lines[index].Indent == indent && lines[index].Content.StartsWith("- ", StringComparison.Ordinal))))
            {
                map[key] = ParseNode(lines, ref index, lines[index].Indent);
            }
            else
            {
                map[key] = null;
            }
        }

        return map;
    }

    private static List<object?> ParseSequence(IReadOnlyList<YamlLine> lines, ref int index, int indent)
    {
        var list = new List<object?>();

        while (index < lines.Count)
        {
            var line = lines[index];
            if (line.Indent < indent)
            {
                break;
            }

            if (line.Indent != indent || !line.Content.StartsWith("- ", StringComparison.Ordinal))
            {
                break;
            }

            var itemText = line.Content[2..].Trim();
            index++;

            if (itemText.Length == 0)
            {
                if (index < lines.Count && lines[index].Indent >= indent)
                {
                    list.Add(ParseNode(lines, ref index, lines[index].Indent));
                }
                else
                {
                    list.Add(null);
                }

                continue;
            }

            var separatorIndex = itemText.IndexOf(':');
            if (separatorIndex > 0)
            {
                var key = itemText[..separatorIndex].Trim();
                var remainder = itemText[(separatorIndex + 1)..].Trim();
                var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [key] = remainder.Length == 0 ? null : ParseScalar(remainder)
                };

                while (index < lines.Count)
                {
                    var next = lines[index];
                    if (next.Indent < indent + 2)
                    {
                        break;
                    }

                    if (next.Indent == indent && next.Content.StartsWith("- ", StringComparison.Ordinal))
                    {
                        break;
                    }

                    if (next.Indent == indent + 2)
                    {
                        var nestedSeparator = next.Content.IndexOf(':');
                        if (nestedSeparator < 0)
                        {
                            throw new InvalidOperationException($"Invalid mapping at line {next.LineNumber}: {next.Content}");
                        }

                        var nestedKey = next.Content[..nestedSeparator].Trim();
                        var nestedRemainder = next.Content[(nestedSeparator + 1)..].Trim();
                        index++;

                        if (nestedRemainder.Length > 0)
                        {
                            map[nestedKey] = ParseScalar(nestedRemainder);
                        }
                        else if (index < lines.Count &&
                                 (lines[index].Indent > next.Indent ||
                                  (lines[index].Indent == next.Indent && lines[index].Content.StartsWith("- ", StringComparison.Ordinal))))
                        {
                            map[nestedKey] = ParseNode(lines, ref index, lines[index].Indent);
                        }
                        else
                        {
                            map[nestedKey] = null;
                        }

                        continue;
                    }

                    break;
                }

                list.Add(map);
                continue;
            }

            list.Add(ParseScalar(itemText));
        }

        return list;
    }

    private static object ParseScalar(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        if (string.Equals(value, "null", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "~", StringComparison.Ordinal))
        {
            return null!;
        }

        if (bool.TryParse(value, out var boolean))
        {
            return boolean;
        }

        if (int.TryParse(value, out var intValue))
        {
            return intValue;
        }

        return value;
    }

    private static List<YamlLine> PrepareLines(string yaml)
    {
        var output = new List<YamlLine>();
        var split = yaml.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        for (var i = 0; i < split.Length; i++)
        {
            var raw = split[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var content = raw.Trim();
            if (content.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var indent = raw.Length - raw.TrimStart().Length;
            output.Add(new YamlLine(indent, content, i + 1));
        }

        return output;
    }

    private readonly record struct YamlLine(int Indent, string Content, int LineNumber);
}
