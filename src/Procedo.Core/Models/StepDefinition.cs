using System.Collections.Generic;

namespace Procedo.Core.Models;

public sealed class StepDefinition
{
    public string Step { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string? Condition { get; set; }

    public string? SourcePath { get; set; }

    public Dictionary<string, object> With { get; set; } = new();

    public List<string> DependsOn { get; set; } = new();

    public int? TimeoutMs { get; set; }

    public int? Retries { get; set; }

    public bool? ContinueOnError { get; set; }
}

