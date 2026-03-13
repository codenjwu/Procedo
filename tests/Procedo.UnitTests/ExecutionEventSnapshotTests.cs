using System.Text.Json;
using Procedo.Observability;

namespace Procedo.UnitTests;

public class ExecutionEventSnapshotTests
{
    [Fact]
    public void RunStarted_Event_Serialization_Should_Match_Snapshot()
    {
        var evt = new ExecutionEvent
        {
            Sequence = 1,
            TimestampUtc = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero),
            EventType = ExecutionEventType.RunStarted,
            RunId = "run-001",
            WorkflowName = "wf",
            Resumed = false
        };

        AssertSnapshot("run_started.json", Serialize(evt));
    }

    [Fact]
    public void StepCompleted_Event_Serialization_Should_Match_Snapshot()
    {
        var evt = new ExecutionEvent
        {
            Sequence = 3,
            TimestampUtc = new DateTimeOffset(2026, 3, 10, 12, 0, 2, TimeSpan.Zero),
            EventType = ExecutionEventType.StepCompleted,
            RunId = "run-001",
            WorkflowName = "wf",
            Stage = "s1",
            Job = "j1",
            StepId = "download",
            StepType = "system.http",
            Success = true,
            DurationMs = 120,
            Outputs = new Dictionary<string, object>
            {
                ["path"] = "/tmp/file.txt"
            }
        };

        AssertSnapshot("step_completed.json", Serialize(evt));
    }

    [Fact]
    public void StepSkipped_Event_Serialization_Should_Match_Snapshot()
    {
        var evt = new ExecutionEvent
        {
            Sequence = 2,
            TimestampUtc = new DateTimeOffset(2026, 3, 10, 12, 0, 1, TimeSpan.Zero),
            EventType = ExecutionEventType.StepSkipped,
            RunId = "run-001",
            WorkflowName = "wf",
            Stage = "s1",
            Job = "j1",
            StepId = "parse",
            StepType = "system.parse",
            Success = true,
            Resumed = true
        };

        AssertSnapshot("step_skipped.json", Serialize(evt));
    }

    [Fact]
    public void Serialization_Should_Keep_Stable_Property_Order()
    {
        var json = Serialize(new ExecutionEvent
        {
            Sequence = 1,
            TimestampUtc = new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero),
            EventType = ExecutionEventType.RunStarted,
            RunId = "run-001",
            WorkflowName = "wf"
        });

        var orderedKeys = new[]
        {
            "\"Sequence\"",
            "\"TimestampUtc\"",
            "\"EventType\"",
            "\"SchemaVersion\"",
            "\"RunId\"",
            "\"WorkflowName\"",
            "\"Stage\"",
            "\"Job\"",
            "\"StepId\"",
            "\"StepType\"",
            "\"Success\"",
            "\"Resumed\"",
            "\"WaitType\"",
            "\"WaitKey\"",
            "\"SignalType\"",
            "\"DurationMs\"",
            "\"Error\"",
            "\"SourcePath\"",
            "\"Outputs\""
        };

        var last = -1;
        foreach (var key in orderedKeys)
        {
            var idx = json.IndexOf(key, StringComparison.Ordinal);
            Assert.True(idx > last, $"Property '{key}' should appear after previous property. JSON: {json}");
            last = idx;
        }
    }

    private static string Serialize(ExecutionEvent evt)
        => JsonSerializer.Serialize(evt, new JsonSerializerOptions { WriteIndented = false });

    private static void AssertSnapshot(string fileName, string actual)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Snapshots", fileName);
        var expected = File.ReadAllText(path).Trim();
        Assert.Equal(expected, actual);
    }
}

