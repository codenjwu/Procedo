namespace Procedo.Observability;

public sealed class ExecutionEvent
{
    public long Sequence { get; set; }

    public DateTimeOffset TimestampUtc { get; set; }

    public ExecutionEventType EventType { get; set; }

    public int SchemaVersion { get; set; } = 1;

    public string RunId { get; set; } = string.Empty;

    public string? WorkflowName { get; set; }

    public string? Stage { get; set; }

    public string? Job { get; set; }

    public string? StepId { get; set; }

    public string? StepType { get; set; }

    public bool? Success { get; set; }

    public bool? Resumed { get; set; }

    public string? WaitType { get; set; }

    public string? WaitKey { get; set; }

    public string? SignalType { get; set; }

    public long? DurationMs { get; set; }

    public string? Error { get; set; }

    public string? SourcePath { get; set; }

    public Dictionary<string, object>? Outputs { get; set; }
}
