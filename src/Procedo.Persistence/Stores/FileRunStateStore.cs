using System.Text.Json;
using Procedo.Core.Abstractions;
using Procedo.Core.Runtime;

namespace Procedo.Persistence.Stores;

public sealed class FileRunStateStore : IRunStateStore, IWaitingRunQueryStore, IConditionalRunStateStore
{
    public const int CurrentPersistenceSchemaVersion = 2;

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

    public async Task<IReadOnlyList<ActiveWaitState>> FindWaitingRunsAsync(WaitingRunQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        var matches = new List<ActiveWaitState>();
        foreach (var path in Directory.EnumerateFiles(_rootPath, "*.json", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var runId = Path.GetFileNameWithoutExtension(path);
            var run = await GetRunAsync(runId, cancellationToken).ConfigureAwait(false);
            if (run is null || run.Status != RunStatus.Waiting)
            {
                continue;
            }

            var activeWait = ActiveWaitStateProjector.TryProject(run, includeMetadata: query.IncludeMetadata);
            if (activeWait is null || !Matches(activeWait, query))
            {
                continue;
            }

            matches.Add(activeWait);
        }

        matches.Sort(CompareNewestFirst);
        if (query.Limit is > 0 && matches.Count > query.Limit.Value)
        {
            return matches.Take(query.Limit.Value).ToArray();
        }

        return matches;
    }

    public async Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default)
    {
        if (runState is null)
        {
            throw new ArgumentNullException(nameof(runState));
        }

        await SaveRunCoreAsync(runState, expectedVersion: null, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> TrySaveRunAsync(WorkflowRunState runState, long expectedVersion, CancellationToken cancellationToken = default)
    {
        if (runState is null)
        {
            throw new ArgumentNullException(nameof(runState));
        }

        return SaveRunCoreAsync(runState, expectedVersion, cancellationToken);
    }

    public Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default)
        => DeleteRunCoreAsync(runId, cancellationToken);

    private string GetRunPath(string runId) => Path.Combine(_rootPath, $"{runId}.json");

    private string GetRunLockPath(string runId) => Path.Combine(_rootPath, $"{runId}.lock");

    private async Task<bool> DeleteRunCoreAsync(string runId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRunId(runId);

        var path = GetRunPath(runId);
        var existed = File.Exists(path);
        if (!existed && !File.Exists(GetRunLockPath(runId)))
        {
            return false;
        }

        await using var deleteLock = await AcquireRunLockAsync(runId, cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                existed = true;
            }
        }
        finally
        {
            await deleteLock.DisposeAsync().ConfigureAwait(false);
            TryDeleteLockFile(runId);
        }

        return existed;
    }

    private async Task<bool> SaveRunCoreAsync(WorkflowRunState runState, long? expectedVersion, CancellationToken cancellationToken)
    {
        ValidateRunId(runState.RunId);

        var saveLock = await AcquireRunLockAsync(runState.RunId, cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTimeOffset.UtcNow;
            if (runState.CreatedAtUtc == default)
            {
                runState.CreatedAtUtc = now;
            }

            var path = GetRunPath(runState.RunId);
            if (expectedVersion is not null)
            {
                var current = await GetRunAsync(runState.RunId, cancellationToken).ConfigureAwait(false);
                if (current is null || current.ConcurrencyVersion != expectedVersion.Value)
                {
                    return false;
                }
            }

            runState.UpdatedAtUtc = now;
            runState.PersistenceSchemaVersion = CurrentPersistenceSchemaVersion;
            runState.ConcurrencyVersion = expectedVersion is null
                ? Math.Max(runState.ConcurrencyVersion, 0) + 1
                : expectedVersion.Value + 1;

            NormalizeRun(runState, sourcePath: null);

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

            return true;
        }
        finally
        {
            await saveLock.DisposeAsync().ConfigureAwait(false);
            TryDeleteLockFile(runState.RunId);
        }
    }

    private static void NormalizeRun(WorkflowRunState run, string? sourcePath)
    {
        if (run.PersistenceSchemaVersion == 0)
        {
            run.PersistenceSchemaVersion = CurrentPersistenceSchemaVersion;
        }
        else if (run.PersistenceSchemaVersion == 1)
        {
            run.PersistenceSchemaVersion = CurrentPersistenceSchemaVersion;
        }
        else if (run.PersistenceSchemaVersion > CurrentPersistenceSchemaVersion)
        {
            throw new InvalidDataException(
                $"Run state schema version '{run.PersistenceSchemaVersion}' is not supported by this Procedo build.{FormatPathSuffix(sourcePath)}");
        }

        run.Steps ??= new Dictionary<string, StepRunState>(StringComparer.OrdinalIgnoreCase);
        run.WorkflowParameters ??= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        run.WorkflowParameters = NormalizeDictionary(run.WorkflowParameters);

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

    private static Dictionary<string, object> NormalizeDictionary(IDictionary<string, object> values)
    {
        var converted = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            converted[key] = ConvertValue(value)!;
        }

        return converted;
    }

    private static bool Matches(ActiveWaitState wait, WaitingRunQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.WorkflowName)
            && !string.Equals(wait.WorkflowName, query.WorkflowName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.WaitType)
            && !string.Equals(wait.WaitType, query.WaitType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.WaitKey)
            && !string.Equals(wait.WaitKey, query.WaitKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.StepId)
            && !string.Equals(wait.StepId, query.StepId, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.ExpectedSignalType)
            && !string.Equals(wait.ExpectedSignalType, query.ExpectedSignalType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static int CompareNewestFirst(ActiveWaitState left, ActiveWaitState right)
    {
        var waitingSince = Nullable.Compare(right.WaitingSinceUtc, left.WaitingSinceUtc);
        if (waitingSince != 0)
        {
            return waitingSince;
        }

        return string.Compare(left.RunId, right.RunId, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<FileStream> AcquireRunLockAsync(string runId, CancellationToken cancellationToken)
    {
        var lockPath = GetRunLockPath(runId);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(25, cancellationToken).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(25, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private void TryDeleteLockFile(string runId)
    {
        var lockPath = GetRunLockPath(runId);
        if (!File.Exists(lockPath))
        {
            return;
        }

        try
        {
            File.Delete(lockPath);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static object? ConvertValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is JsonElement json)
        {
            return ConvertElement(json);
        }

        return value;
    }

    private static object? ConvertElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var i64) => i64,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                p => p.Name,
                p => ConvertElement(p.Value),
                StringComparer.OrdinalIgnoreCase),
            _ => element.ToString() ?? string.Empty
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


