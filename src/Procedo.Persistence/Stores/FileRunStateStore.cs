using System.Text.Json;
using Procedo.Core.Abstractions;
using Procedo.Core.Runtime;

namespace Procedo.Persistence.Stores;

public sealed class FileRunStateStore : IRunStateStore
{
    public const int CurrentPersistenceSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _rootPath;

    public FileRunStateStore(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("A root path is required.", nameof(rootPath));
        }

        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<WorkflowRunState?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        ValidateRunId(runId);

        var path = GetRunPath(runId);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var run = await JsonSerializer.DeserializeAsync<WorkflowRunState>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (run is null)
            {
                throw new InvalidDataException($"Run state file '{path}' did not contain a valid workflow run payload.");
            }

            NormalizeRun(run, path);
            return run;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Run state file '{path}' is malformed JSON.", ex);
        }
    }

    public async Task<IReadOnlyList<WorkflowRunState>> ListRunsAsync(CancellationToken cancellationToken = default)
    {
        var runs = new List<WorkflowRunState>();
        foreach (var path in Directory.EnumerateFiles(_rootPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runId = Path.GetFileNameWithoutExtension(path);
            var run = await GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
            if (run is not null)
            {
                runs.Add(run);
            }
        }

        runs.Sort(static (left, right) => Nullable.Compare(right.WaitingSinceUtc, left.WaitingSinceUtc));
        return runs;
    }
    public async Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default)
    {
        if (runState is null)
        {
            throw new ArgumentNullException(nameof(runState));
        }

        ValidateRunId(runState.RunId);

        var now = DateTimeOffset.UtcNow;
        if (runState.CreatedAtUtc == default)
        {
            runState.CreatedAtUtc = now;
        }

        runState.UpdatedAtUtc = now;
        runState.PersistenceSchemaVersion = CurrentPersistenceSchemaVersion;

        NormalizeRun(runState, sourcePath: null);

        var path = GetRunPath(runState.RunId);
        var tempPath = Path.Combine(_rootPath, $"{runState.RunId}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, runState, JsonOptions, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (File.Exists(path))
            {
                File.Replace(tempPath, path, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
    public Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRunId(runId);

        var path = GetRunPath(runId);
        if (!File.Exists(path))
        {
            return Task.FromResult(false);
        }

        File.Delete(path);
        return Task.FromResult(true);
    }

    private string GetRunPath(string runId) => Path.Combine(_rootPath, $"{runId}.json");

    private static void NormalizeRun(WorkflowRunState run, string? sourcePath)
    {
        if (run.PersistenceSchemaVersion == 0)
        {
            run.PersistenceSchemaVersion = CurrentPersistenceSchemaVersion;
        }
        else if (run.PersistenceSchemaVersion > CurrentPersistenceSchemaVersion)
        {
            throw new InvalidDataException(
                $"Run state schema version '{run.PersistenceSchemaVersion}' is not supported by this Procedo build.{FormatPathSuffix(sourcePath)}");
        }

        run.Steps ??= new Dictionary<string, StepRunState>(StringComparer.OrdinalIgnoreCase);

        var normalizedSteps = new Dictionary<string, StepRunState>(StringComparer.OrdinalIgnoreCase);

        foreach (var (stepKey, stepValue) in run.Steps)
        {
            var step = stepValue ?? new StepRunState();
            step.Outputs ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            var converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in step.Outputs)
            {
                converted[key] = ConvertValue(value);
            }

            step.Outputs = converted;
            step.Wait = NormalizeWait(step.Wait);
            normalizedSteps[stepKey] = step;
        }

        run.Steps = normalizedSteps;
    }

    private static WaitDescriptor? NormalizeWait(WaitDescriptor? wait)
    {
        if (wait is null)
        {
            return null;
        }

        wait.Metadata ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        var converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in wait.Metadata)
        {
            converted[key] = ConvertValue(value);
        }

        wait.Metadata = converted;
        return wait;
    }

    private static object ConvertValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is JsonElement json)
        {
            return ConvertElement(json);
        }

        return value;
    }

    private static object ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number when element.TryGetInt64(out var i64) => i64,
            JsonValueKind.Number when element.TryGetDouble(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                p => p.Name,
                p => ConvertElement(p.Value),
                StringComparer.OrdinalIgnoreCase),
            _ => element.ToString()
        };
    }

    private static string FormatPathSuffix(string? sourcePath)
        => string.IsNullOrWhiteSpace(sourcePath)
            ? string.Empty
            : $" File: '{sourcePath}'.";

    private static void ValidateRunId(string runId)
    {
        if (string.IsNullOrWhiteSpace(runId))
        {
            throw new ArgumentException("RunId is required.", nameof(runId));
        }

        if (runId.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || runId.Contains(Path.DirectorySeparatorChar)
            || runId.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException("RunId contains invalid path characters.", nameof(runId));
        }
    }
}


