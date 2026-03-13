using System.Threading;

namespace Procedo.Observability;

public sealed class ExecutionEventPublisher
{
    private readonly IExecutionEventSink _sink;
    private long _sequence;

    public ExecutionEventPublisher(IExecutionEventSink? sink = null)
    {
        _sink = sink ?? Sinks.NullExecutionEventSink.Instance;
    }

    public async Task PublishAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
    {
        executionEvent.Sequence = Interlocked.Increment(ref _sequence);
        executionEvent.TimestampUtc = DateTimeOffset.UtcNow;
        executionEvent = ExecutionEventSanitizer.Sanitize(executionEvent);

        try
        {
            await _sink.WriteAsync(executionEvent, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Sink errors must never affect workflow execution.
        }
    }
}

