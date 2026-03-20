using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class WorkflowRunState
{
    public int PersistenceSchemaVersion { get; set; } = 2;

    public long ConcurrencyVersion { get; set; }

    public string RunId { get; set; } = string.Empty;

    public string WorkflowName { get; set; } = string.Empty;

    public int WorkflowVersion { get; set; }

    public string? WorkflowSourcePath { get; set; }

    public string? WorkflowDefinitionSnapshot { get; set; }

    public string? WorkflowDefinitionFingerprint { get; set; }

    public Dictionary<string, object> WorkflowParameters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public RunStatus Status { get; set; } = RunStatus.Pending;

    public string? Error { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }

    public string? WaitingStepKey { get; set; }

    public DateTimeOffset? WaitingSinceUtc { get; set; }

    public Dictionary<string, StepRunState> Steps { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
