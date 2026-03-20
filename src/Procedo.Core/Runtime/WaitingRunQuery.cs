namespace Procedo.Core.Runtime;

public sealed class WaitingRunQuery
{
    public string? WorkflowName { get; set; }

    public string? WaitType { get; set; }

    public string? WaitKey { get; set; }

    public string? StepId { get; set; }

    public string? ExpectedSignalType { get; set; }

    public bool IncludeMetadata { get; set; } = true;

    public int? Limit { get; set; }
}
