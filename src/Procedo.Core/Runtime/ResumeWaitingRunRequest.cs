using System.Collections.Generic;

namespace Procedo.Core.Runtime;

public sealed class ResumeWaitingRunRequest
{
    public string? WorkflowName { get; set; }

    public string WaitType { get; set; } = string.Empty;

    public string? WaitKey { get; set; }

    public string? StepId { get; set; }

    public string? ExpectedSignalType { get; set; }

    public string SignalType { get; set; } = string.Empty;

    public IDictionary<string, object> Payload { get; set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public WaitingRunMatchBehavior MatchBehavior { get; set; } = WaitingRunMatchBehavior.FailWhenMultiple;
}
