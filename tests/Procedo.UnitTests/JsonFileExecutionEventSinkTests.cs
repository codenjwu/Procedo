using System.Text.Json;
using Procedo.Observability;
using Procedo.Observability.Sinks;

namespace Procedo.UnitTests;

public class JsonFileExecutionEventSinkTests
{
    [Fact]
    public async Task WriteAsync_Should_Append_Json_Line_Per_Event()
    {
        var file = CreateTempFilePath();
        try
        {
            var sink = new JsonFileExecutionEventSink(file);

            await sink.WriteAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunStarted,
                RunId = "r1",
                WorkflowName = "wf"
            });

            await sink.WriteAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunCompleted,
                RunId = "r1",
                WorkflowName = "wf",
                Success = true
            });

            var lines = await File.ReadAllLinesAsync(file);
            Assert.Equal(2, lines.Length);

            var first = JsonSerializer.Deserialize<ExecutionEvent>(lines[0]);
            var second = JsonSerializer.Deserialize<ExecutionEvent>(lines[1]);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.Equal(ExecutionEventType.RunStarted, first!.EventType);
            Assert.Equal(ExecutionEventType.RunCompleted, second!.EventType);
            Assert.Equal("r1", second.RunId);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_Should_Handle_Large_Output_Payload()
    {
        var file = CreateTempFilePath();
        try
        {
            var sink = new JsonFileExecutionEventSink(file);
            var large = new string('x', 200_000);

            await sink.WriteAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.StepCompleted,
                RunId = "r-big",
                WorkflowName = "wf",
                Stage = "s1",
                Job = "j1",
                StepId = "a",
                StepType = "system.echo",
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["blob"] = large
                }
            });

            var line = await File.ReadAllTextAsync(file);
            Assert.Contains("r-big", line, StringComparison.Ordinal);
            Assert.Contains(large[..100], line, StringComparison.Ordinal);
            Assert.True(line.Length > 200_000);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task WriteAsync_Should_Not_Write_Partial_Line_When_Cancelled()
    {
        var file = CreateTempFilePath();
        try
        {
            var sink = new JsonFileExecutionEventSink(file);

            await sink.WriteAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunStarted,
                RunId = "r1",
                WorkflowName = "wf"
            });

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sink.WriteAsync(new ExecutionEvent
            {
                EventType = ExecutionEventType.RunCompleted,
                RunId = "r1",
                WorkflowName = "wf",
                Success = true
            }, cts.Token));

            var lines = await File.ReadAllLinesAsync(file);
            Assert.Single(lines);
            var evt = JsonSerializer.Deserialize<ExecutionEvent>(lines[0]);
            Assert.Equal(ExecutionEventType.RunStarted, evt!.EventType);
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private static string CreateTempFilePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "events.jsonl");
    }
}
