namespace Procedo.Core.Execution;

public sealed class WorkflowExecutionOptions
{
    public static WorkflowExecutionOptions Default => new();

    public int DefaultMaxParallelism { get; set; } = 1;

    public int DefaultStepRetries { get; set; } = 0;

    public int? DefaultStepTimeoutMs { get; set; }

    public bool ContinueOnError { get; set; }

    public int RetryInitialBackoffMs { get; set; } = 200;

    public double RetryBackoffMultiplier { get; set; } = 2.0;

    public int RetryMaxBackoffMs { get; set; } = 5000;

    public int GetSafeDefaultMaxParallelism() => DefaultMaxParallelism > 0 ? DefaultMaxParallelism : 1;

    public int GetSafeDefaultRetries() => DefaultStepRetries >= 0 ? DefaultStepRetries : 0;

    public int? GetSafeDefaultTimeoutMs() => DefaultStepTimeoutMs is > 0 ? DefaultStepTimeoutMs : null;

    public int GetSafeRetryInitialBackoffMs() => RetryInitialBackoffMs > 0 ? RetryInitialBackoffMs : 1;

    public double GetSafeRetryBackoffMultiplier() => RetryBackoffMultiplier > 0 ? RetryBackoffMultiplier : 2.0;

    public int GetSafeRetryMaxBackoffMs() => RetryMaxBackoffMs > 0 ? RetryMaxBackoffMs : 5000;
}
