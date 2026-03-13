using System.Text.Json;
using Procedo.Observability;

namespace Procedo.ContractTests;

public class ExecutionEventCrossTargetContractTests
{
    [Fact]
    public void Serialize_RunStarted_Should_Keep_Contract_Stable()
    {
        var evt = new ExecutionEvent
        {
            Sequence = 1,
            TimestampUtc = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero),
            EventType = ExecutionEventType.RunStarted,
            SchemaVersion = 1,
            RunId = "run-001",
            WorkflowName = "wf",
            Resumed = false
        };

        var actual = JsonSerializer.Serialize(evt);
        const string expected = "{\"Sequence\":1,\"TimestampUtc\":\"2026-03-10T12:00:00+00:00\",\"EventType\":0,\"SchemaVersion\":1,\"RunId\":\"run-001\",\"WorkflowName\":\"wf\",\"Stage\":null,\"Job\":null,\"StepId\":null,\"StepType\":null,\"Success\":null,\"Resumed\":false,\"WaitType\":null,\"WaitKey\":null,\"SignalType\":null,\"DurationMs\":null,\"Error\":null,\"SourcePath\":null,\"Outputs\":null}";

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Deserialize_With_Unknown_Fields_Should_Be_Tolerant()
    {
        const string payload = "{\"Sequence\":1,\"TimestampUtc\":\"2026-03-10T12:00:00+00:00\",\"EventType\":0,\"SchemaVersion\":1,\"RunId\":\"run-001\",\"WorkflowName\":\"wf\",\"Extra\":\"ignored\"}";

        var evt = JsonSerializer.Deserialize<ExecutionEvent>(payload);

        Assert.NotNull(evt);
        Assert.Equal("run-001", evt!.RunId);
        Assert.Equal(1, evt.SchemaVersion);
    }
}

