using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class ActiveWaitState
{
    public string RunId { get; set; } = string.Empty;

    public string WorkflowName { get; set; } = string.Empty;

    public RunStatus RunStatus { get; set; }

    public DateTimeOffset? WaitingSinceUtc { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public string StepId { get; set; } = string.Empty;

    public string StepPath { get; set; } = string.Empty;

    public string WaitType { get; set; } = string.Empty;

    public string? WaitKey { get; set; }

    public string? WaitReason { get; set; }

    public string? ExpectedSignalType { get; set; }

    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
}
