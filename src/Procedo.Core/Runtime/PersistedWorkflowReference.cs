using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class PersistedWorkflowReference
{
    public string RunId { get; set; } = string.Empty;

    public string WorkflowName { get; set; } = string.Empty;

    public int WorkflowVersion { get; set; }

    public string? WorkflowSourcePath { get; set; }

    public string? WorkflowDefinitionSnapshot { get; set; }

    public string? WorkflowDefinitionFingerprint { get; set; }

    public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
