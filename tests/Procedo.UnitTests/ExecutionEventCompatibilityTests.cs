using System.Text.Json;
using Procedo.Observability;

namespace Procedo.UnitTests;

public class ExecutionEventCompatibilityTests
{
    [Fact]
    public void Deserialize_Legacy_Json_Without_SchemaVersion_Should_Default_To_One()
    {
        const string legacy = "{\"Sequence\":1,\"TimestampUtc\":\"2026-03-10T12:00:00+00:00\",\"EventType\":0,\"RunId\":\"r\",\"WorkflowName\":\"wf\"}";

        var evt = JsonSerializer.Deserialize<ExecutionEvent>(legacy);

        Assert.NotNull(evt);
        Assert.Equal(1, evt!.Sequence);
        Assert.Equal(ExecutionEventType.RunStarted, evt.EventType);
        Assert.Equal(1, evt.SchemaVersion);
    }

    [Fact]
    public void Deserialize_With_Unknown_Fields_Should_Succeed()
    {
        const string withUnknown = "{\"Sequence\":2,\"TimestampUtc\":\"2026-03-10T12:00:01+00:00\",\"EventType\":6,\"SchemaVersion\":1,\"RunId\":\"r\",\"WorkflowName\":\"wf\",\"UnknownField\":\"x\",\"Another\":123}";

        var evt = JsonSerializer.Deserialize<ExecutionEvent>(withUnknown);

        Assert.NotNull(evt);
        Assert.Equal(ExecutionEventType.StepSkipped, evt!.EventType);
        Assert.Equal("r", evt.RunId);
        Assert.Equal(1, evt.SchemaVersion);
    }

    [Fact]
    public void Deserialize_With_Higher_SchemaVersion_Should_Preserve_Value()
    {
        const string nextVersion = "{\"Sequence\":10,\"TimestampUtc\":\"2026-03-10T12:00:10+00:00\",\"EventType\":1,\"SchemaVersion\":2,\"RunId\":\"r2\",\"WorkflowName\":\"wf2\",\"Success\":true}";

        var evt = JsonSerializer.Deserialize<ExecutionEvent>(nextVersion);

        Assert.NotNull(evt);
        Assert.Equal(2, evt!.SchemaVersion);
        Assert.Equal(ExecutionEventType.RunCompleted, evt.EventType);
    }
}
