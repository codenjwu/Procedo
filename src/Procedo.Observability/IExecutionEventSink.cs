namespace Procedo.Observability;

public interface IExecutionEventSink
{
    Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default);
}
