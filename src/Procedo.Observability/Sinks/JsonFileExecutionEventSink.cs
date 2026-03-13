using System.Text.Json;

namespace Procedo.Observability.Sinks;

public sealed class JsonFileExecutionEventSink : IExecutionEventSink
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public JsonFileExecutionEventSink(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public async Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
    {
        var line = JsonSerializer.Serialize(executionEvent, JsonOptions) + Environment.NewLine;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.AppendAllTextAsync(_filePath, line, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
