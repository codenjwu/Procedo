using Procedo.Core.Abstractions;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Persistence.Stores;
using Procedo.Plugin.System;

if (HasFlag(args, "--help", "-h"))
{
    PrintHelp();
    return 0;
}

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var options = ParseOptions(args);
var workflowPath = ResolvePath(GetOption(options, "workflow"), Path.Combine(repoRoot, "examples", "71_callback_resume_identity_demo.yaml"), repoRoot);
var stateDirectory = ResolvePath(GetOption(options, "state-dir"), Path.Combine(repoRoot, ".procedo", "custom-resolver-store"), repoRoot);
var waitType = GetOption(options, "wait-type") ?? "signal";
var waitKey = GetOption(options, "wait-key") ?? "callback-identity-demo";
var expectedSignal = GetOption(options, "expected-signal") ?? "approve";
var signalType = GetOption(options, "signal-type") ?? expectedSignal;

TryDeleteDirectory(stateDirectory);
Directory.CreateDirectory(stateDirectory);

var innerStore = new FileRunStateStore(stateDirectory);
var loggingStore = new LoggingRunStateStore(innerStore);
var resolver = new LoggingWorkflowDefinitionResolver(new FileWorkflowDefinitionResolver());

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .UseRunStateStore(loggingStore)
    .UseWorkflowDefinitionResolver(resolver)
    .Build();

var first = await host.ExecuteFileAsync(workflowPath).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");
if (!first.Waiting)
{
    return first.Success ? 0 : 1;
}

var waits = await host.FindWaitingRunsAsync(new WaitingRunQuery
{
    WaitType = waitType,
    WaitKey = waitKey,
    ExpectedSignalType = expectedSignal
}).ConfigureAwait(false);

Console.WriteLine($"Custom store wait matches: {waits.Count}");

var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
{
    WaitType = waitType,
    WaitKey = waitKey,
    ExpectedSignalType = expectedSignal,
    SignalType = signalType,
    Payload = new Dictionary<string, object>
    {
        ["approved_by"] = "custom-store-host",
        ["ticket"] = "CHG-721"
    }
}).ConfigureAwait(false);

Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
Console.WriteLine($"Store metrics: saves={loggingStore.SaveCount}, conditionalSaves={loggingStore.ConditionalSaveCount}, queries={loggingStore.QueryCount}");
Console.WriteLine($"Resolver metrics: resolutions={resolver.ResolveCount}");
return resumed.Success ? 0 : 1;

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            continue;
        }

        if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
        {
            options[arg[2..]] = args[++i];
        }
        else
        {
            options[arg[2..]] = null;
        }
    }

    return options;
}

static string? GetOption(IReadOnlyDictionary<string, string?> options, string name)
    => options.TryGetValue(name, out var value) ? value : null;

static bool HasFlag(string[] args, params string[] flags)
    => args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));

static string ResolvePath(string? value, string defaultPath, string repoRoot)
{
    var candidate = string.IsNullOrWhiteSpace(value) ? defaultPath : value;
    return Path.IsPathRooted(candidate) ? candidate : Path.GetFullPath(Path.Combine(repoRoot, candidate));
}

static void PrintHelp()
{
    Console.WriteLine("Procedo.Example.CustomResolverStore");
    Console.WriteLine();
    Console.WriteLine("Demonstrates custom run-state-store and workflow-resolver capability wiring using public interfaces.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.CustomResolverStore");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.CustomResolverStore -- --workflow examples/71_callback_resume_identity_demo.yaml --wait-key callback-identity-demo --signal-type approve");
}

static void TryDeleteDirectory(string path)
{
    try { Directory.Delete(path, true); } catch { }
}

static string FindRepoRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        if (File.Exists(Path.Combine(current.FullName, "Procedo.sln")))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root (Procedo.sln).");
}

internal sealed class LoggingRunStateStore : IRunStateStore, IConditionalRunStateStore, IWaitingRunQueryStore
{
    private readonly FileRunStateStore _inner;

    public LoggingRunStateStore(FileRunStateStore inner)
    {
        _inner = inner;
    }

    public int SaveCount { get; private set; }

    public int ConditionalSaveCount { get; private set; }

    public int QueryCount { get; private set; }

    public Task<WorkflowRunState?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
        => _inner.GetRunAsync(runId, cancellationToken);

    public Task<IReadOnlyList<WorkflowRunState>> ListRunsAsync(CancellationToken cancellationToken = default)
        => _inner.ListRunsAsync(cancellationToken);

    public async Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        Console.WriteLine($"[custom-store] SaveRunAsync run={runState.RunId} status={runState.Status}");
        await _inner.SaveRunAsync(runState, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default)
        => _inner.DeleteRunAsync(runId, cancellationToken);

    public async Task<bool> TrySaveRunAsync(WorkflowRunState runState, long expectedVersion, CancellationToken cancellationToken = default)
    {
        ConditionalSaveCount++;
        Console.WriteLine($"[custom-store] TrySaveRunAsync run={runState.RunId} expectedVersion={expectedVersion}");
        return await _inner.TrySaveRunAsync(runState, expectedVersion, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ActiveWaitState>> FindWaitingRunsAsync(WaitingRunQuery query, CancellationToken cancellationToken = default)
    {
        QueryCount++;
        Console.WriteLine($"[custom-store] FindWaitingRunsAsync waitType={query.WaitType} waitKey={query.WaitKey}");
        return await _inner.FindWaitingRunsAsync(query, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class LoggingWorkflowDefinitionResolver : IWorkflowDefinitionResolver
{
    private readonly IWorkflowDefinitionResolver _inner;

    public LoggingWorkflowDefinitionResolver(IWorkflowDefinitionResolver inner)
    {
        _inner = inner;
    }

    public int ResolveCount { get; private set; }

    public async Task<WorkflowDefinition> ResolveAsync(PersistedWorkflowReference reference, CancellationToken cancellationToken = default)
    {
        ResolveCount++;
        Console.WriteLine($"[custom-resolver] ResolveAsync run={reference.RunId} workflow={reference.WorkflowName} fingerprint={reference.WorkflowDefinitionFingerprint}");
        return await _inner.ResolveAsync(reference, cancellationToken).ConfigureAwait(false);
    }
}
