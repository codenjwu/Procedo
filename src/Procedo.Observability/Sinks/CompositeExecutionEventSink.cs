namespace Procedo.Observability.Sinks;

public sealed class CompositeExecutionEventSink : IExecutionEventSink
{
    private readonly IReadOnlyList<IExecutionEventSink> _sinks;

    public CompositeExecutionEventSink(IEnumerable<IExecutionEventSink> sinks)
    {
        _sinks = sinks.ToList();
    }

    public async Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
    {
        foreach (var sink in _sinks)
        {
            try
            {
                await sink.WriteAsync(executionEvent, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Event sink failures must not break workflow execution.
            }
        }
    }
}
