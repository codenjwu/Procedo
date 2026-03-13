using Procedo.Observability;

namespace Procedo.UnitTests;

public class ExecutionEventSanitizerTests
{
    [Fact]
    public async Task PublishAsync_Should_Redact_Resume_Payload_And_Sensitive_Output_Values()
    {
        var sink = new InMemorySink();
        var publisher = new ExecutionEventPublisher(sink);

        await publisher.PublishAsync(new ExecutionEvent
        {
            EventType = ExecutionEventType.StepCompleted,
            RunId = "run-123",
            WorkflowName = "wf",
            Stage = "s1",
            Job = "j1",
            StepId = "wait",
            StepType = "system.wait_signal",
            Success = true,
            Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["payload"] = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["approved_by"] = "operator",
                    ["token"] = "secret-token"
                },
                ["api_key"] = "12345",
                ["message"] = "safe"
            }
        });

        var executionEvent = Assert.Single(sink.Events);
        var outputs = Assert.IsType<Dictionary<string, object>>(executionEvent.Outputs);
        var payload = Assert.IsType<Dictionary<string, object>>(outputs["payload"]);

        Assert.Equal("***REDACTED***", payload["approved_by"]);
        Assert.Equal("***REDACTED***", payload["token"]);
        Assert.Equal("***REDACTED***", outputs["api_key"]);
        Assert.Equal("safe", outputs["message"]);
    }

    private sealed class InMemorySink : IExecutionEventSink
    {
        public List<ExecutionEvent> Events { get; } = new();

        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(executionEvent);
            return Task.CompletedTask;
        }
    }
}
