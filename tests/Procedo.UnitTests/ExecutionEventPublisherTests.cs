using Procedo.Observability;

namespace Procedo.UnitTests;

public class ExecutionEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_Should_Assign_Monotonic_Sequence_And_Timestamp()
    {
        var sink = new InMemorySink();
        var publisher = new ExecutionEventPublisher(sink);

        var first = new ExecutionEvent { EventType = ExecutionEventType.RunStarted, RunId = "r", WorkflowName = "wf" };
        var second = new ExecutionEvent { EventType = ExecutionEventType.RunCompleted, RunId = "r", WorkflowName = "wf" };

        await publisher.PublishAsync(first);
        await publisher.PublishAsync(second);

        Assert.Equal(1, first.Sequence);
        Assert.Equal(2, second.Sequence);
        Assert.True(first.TimestampUtc > DateTimeOffset.MinValue);
        Assert.True(second.TimestampUtc >= first.TimestampUtc);
    }

    [Fact]
    public async Task PublishAsync_Should_Not_Throw_When_Sink_Throws()
    {
        var publisher = new ExecutionEventPublisher(new ThrowingSink());

        var ex = await Record.ExceptionAsync(() => publisher.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = "r",
            WorkflowName = "wf"
        }));

        Assert.Null(ex);
    }

    [Fact]
    public async Task PublishAsync_Should_Not_Throw_When_Cancellation_Is_Requested()
    {
        var publisher = new ExecutionEventPublisher(new DelayedSink());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = await Record.ExceptionAsync(() => publisher.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = "r",
            WorkflowName = "wf"
        }, cts.Token));

        Assert.Null(ex);
    }

    private sealed class InMemorySink : IExecutionEventSink
    {
        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class ThrowingSink : IExecutionEventSink
    {
        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("sink error");
    }

    private sealed class DelayedSink : IExecutionEventSink
    {
        public async Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
        }
    }
}
