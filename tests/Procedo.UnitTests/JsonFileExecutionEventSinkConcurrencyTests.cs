using System.Text.Json;
using Procedo.Observability;
using Procedo.Observability.Sinks;

namespace Procedo.UnitTests;

public class JsonFileExecutionEventSinkConcurrencyTests
{
    [Fact]
    public async Task WriteAsync_Should_Support_Concurrent_Writers_Without_Corrupting_Output()
    {
        var path = CreateTempFilePath();
        try
        {
            var sink = new JsonFileExecutionEventSink(path);
            const int count = 200;

            var tasks = Enumerable.Range(1, count)
                .Select(i => sink.WriteAsync(new ExecutionEvent
                {
                    Sequence = i,
                    TimestampUtc = DateTimeOffset.UtcNow,
                    EventType = ExecutionEventType.StepCompleted,
                    RunId = $"run-{i:D3}",
                    WorkflowName = "wf",
                    Stage = "s",
                    Job = "j",
                    StepId = $"step-{i:D3}",
                    StepType = "system.echo",
                    Success = true,
                    DurationMs = i
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            var lines = await File.ReadAllLinesAsync(path);
            Assert.Equal(count, lines.Length);

            var events = lines
                .Select(line => JsonSerializer.Deserialize<ExecutionEvent>(line))
                .ToList();

            Assert.DoesNotContain(events, e => e is null);
            Assert.Equal(count, events.Select(e => e!.RunId).Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.All(events, e => Assert.Equal(1, e!.SchemaVersion));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static string CreateTempFilePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "events-concurrent.jsonl");
    }
}
