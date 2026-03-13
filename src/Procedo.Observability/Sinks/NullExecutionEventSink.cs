namespace Procedo.Observability.Sinks;

public sealed class NullExecutionEventSink : IExecutionEventSink
{
    public static NullExecutionEventSink Instance { get; } = new();

    private NullExecutionEventSink()
    {
    }

    public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
