using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class StepRunState
{
    public string Stage { get; set; } = string.Empty;

    public string Job { get; set; } = string.Empty;

    public string StepId { get; set; } = string.Empty;

    public StepRunStatus Status { get; set; } = StepRunStatus.Pending;

    public string? Error { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public Dictionary<string, object> Outputs { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public WaitDescriptor? Wait { get; set; }
}
