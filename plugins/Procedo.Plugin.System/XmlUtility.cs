using System.Xml.Linq;

namespace Procedo.Plugin.System;

internal static class XmlUtility
{
    public static string? GetValue(XDocument document, string path)
    {
        var target = Navigate(document.Root, path);
        return target switch
        {
            XElement element => element.Value,
            XAttribute attribute => attribute.Value,
            _ => null
        };
    }

    public static bool SetValue(XDocument document, string path, string value)
    {
        var target = Navigate(document.Root, path, createMissing: true);
        switch (target)
        {
            case XElement element:
                element.Value = value;
                return true;
            case XAttribute attribute:
                attribute.Value = value;
                return true;
            default:
                return false;
        }
    }

    private static object? Navigate(XElement? root, string path, bool createMissing = false)
    {
        if (root is null)
        {
            return null;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        object current = root;

        foreach (var rawSegment in segments)
        {
            if (rawSegment.StartsWith("@", StringComparison.Ordinal))
            {
                if (current is not XElement currentElement)
                {
                    return null;
                }

                var attributeName = rawSegment.Substring(1);
                var attribute = currentElement.Attribute(attributeName);
                if (attribute is null && createMissing)
                {
                    currentElement.SetAttributeValue(attributeName, string.Empty);
                    attribute = currentElement.Attribute(attributeName);
                }

                return attribute;
            }

            var (name, index) = ParseSegment(rawSegment);
            if (current is not XElement parent)
            {
                return null;
            }

            var elements = parent.Elements(name).ToList();
            if (elements.Count <= index)
            {
                if (!createMissing)
                {
                    return null;
                }

                while (elements.Count <= index)
                {
                    parent.Add(new XElement(name));
                    elements = parent.Elements(name).ToList();
                }
            }

            current = elements[index];
        }

        return current;
    }

    private static (string Name, int Index) ParseSegment(string segment)
    {
        var bracketStart = segment.IndexOf('[');
        if (bracketStart < 0 || !segment.EndsWith(']'))
        {
            return (segment, 0);
        }

        var name = segment.Substring(0, bracketStart);
        var indexText = segment.Substring(bracketStart + 1, segment.Length - bracketStart - 2);
        return int.TryParse(indexText, out var index)
            ? (name, index)
            : (name, 0);
    }
}
