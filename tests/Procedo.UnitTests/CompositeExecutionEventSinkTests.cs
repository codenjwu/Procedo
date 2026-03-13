using Procedo.Observability;
using Procedo.Observability.Sinks;

namespace Procedo.UnitTests;

public class CompositeExecutionEventSinkTests
{
    [Fact]
    public async Task WriteAsync_Should_Continue_When_One_Sink_Throws()
    {
        var collected = new List<ExecutionEvent>();
        var sink = new CompositeExecutionEventSink(
        [
            new ThrowingSink(),
            new CollectSink(collected)
        ]);

        await sink.WriteAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = "run-1"
        });

        Assert.Single(collected);
        Assert.Equal(ExecutionEventType.RunStarted, collected[0].EventType);
    }

    private sealed class ThrowingSink : IExecutionEventSink
    {
        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("sink failed");
    }

    private sealed class CollectSink(List<ExecutionEvent> target) : IExecutionEventSink
    {
        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            target.Add(executionEvent);
            return Task.CompletedTask;
        }
    }
}
