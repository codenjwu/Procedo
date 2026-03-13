namespace Procedo.Core.Models;

public sealed class WorkflowRunResult
{
    public bool Success { get; set; }

    public bool Waiting { get; set; }

    public string? Error { get; set; }

    public string? ErrorCode { get; set; }

    public string? RunId { get; set; }

    public string? WaitingStepId { get; set; }

    public string? WaitingType { get; set; }

    public string? SourcePath { get; set; }
}
