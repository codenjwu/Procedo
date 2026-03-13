using System.Collections.Generic;

namespace Procedo.Core.Models;

public sealed class ParameterDefinition
{
    public string Type { get; set; } = "string";

    public bool Required { get; set; }

    public object? Default { get; set; }

    public string? Description { get; set; }

    public List<object> AllowedValues { get; set; } = new();

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public int? MinLength { get; set; }

    public int? MaxLength { get; set; }

    public string? Pattern { get; set; }

    public string? ItemType { get; set; }

    public List<string> RequiredProperties { get; set; } = new();
}
