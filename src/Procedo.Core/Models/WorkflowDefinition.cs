using System.Collections.Generic;

namespace Procedo.Core.Models;

public sealed class WorkflowDefinition
{
    public string Name { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string? Template { get; set; }

    public string? SourcePath { get; set; }

    public string? StageSourcePath { get; set; }

    public int? MaxParallelism { get; set; }

    public bool? ContinueOnError { get; set; }

    public Dictionary<string, ParameterDefinition> ParameterDefinitions { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, object> ParameterValues { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, object> Variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> ParameterDefinitionSources { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> ParameterValueSources { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> VariableSources { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public List<StageDefinition> Stages { get; set; } = new();
}
