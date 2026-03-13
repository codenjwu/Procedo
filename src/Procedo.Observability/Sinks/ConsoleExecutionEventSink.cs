using System.Text.Json;

namespace Procedo.Observability.Sinks;

public sealed class ConsoleExecutionEventSink : IExecutionEventSink
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine(JsonSerializer.Serialize(executionEvent, JsonOptions));
        return Task.CompletedTask;
    }
}
