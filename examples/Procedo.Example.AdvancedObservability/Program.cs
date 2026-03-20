using System.Text.Json;
using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Observability;
using Procedo.Observability.Sinks;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
if (HasFlag(args, "--help", "-h"))
{
    PrintHelp(repoRoot);
    return 0;
}

var options = ParseOptions(args);
var defaultWorkflowPath = Path.Combine(repoRoot, "examples", "78_template_persisted_resume_observability_demo.yaml");
var workflowPath = ResolvePath(GetOption(options, "workflow"), defaultWorkflowPath, repoRoot);
var isDefaultWorkflow = string.Equals(workflowPath, defaultWorkflowPath, StringComparison.OrdinalIgnoreCase);

var defaultStateDirectory = Path.Combine(repoRoot, ".procedo", "example-advanced-observability");
var stateDirectory = ResolvePath(GetOption(options, "state-dir"), defaultStateDirectory, repoRoot);
var eventPath = Path.Combine(stateDirectory, "events.jsonl");
if (!HasFlag(args, "--keep-state"))
{
    TryDeleteDirectory(stateDirectory);
}

Directory.CreateDirectory(stateDirectory);

var sink = new CompositeExecutionEventSink(new IExecutionEventSink[]
{
    new ConsoleExecutionEventSink(),
    new JsonFileExecutionEventSink(eventPath)
});

var runId = GetOption(options, "run-id") ?? "advanced-observability-demo";
var resumeSignal = GetOption(options, "resume-signal") ?? (isDefaultWorkflow ? "approve" : null);
var resumePayload = ParsePayload(GetOption(options, "resume-payload-json"), new Dictionary<string, object>
{
    ["ticket"] = "CHG-780",
    ["approved_by"] = "ops-observer"
});

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .UseEventSink(sink)
    .UseLocalRunStateStore(stateDirectory, runId)
    .ConfigureExecution(static execution =>
    {
        execution.DefaultMaxParallelism = 4;
        execution.DefaultStepRetries = 2;
        execution.RetryInitialBackoffMs = 25;
        execution.RetryMaxBackoffMs = 200;
    })
    .Build();

var first = await host.ExecuteFileAsync(workflowPath).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");
if (!first.Waiting)
{
    Console.WriteLine($"Events file: {eventPath}");
    return first.Success ? 0 : 1;
}

if (string.IsNullOrWhiteSpace(resumeSignal))
{
    Console.WriteLine("Workflow is waiting. Supply --resume-signal to continue a non-default resumable scenario.");
    Console.WriteLine($"Events file: {eventPath}");
    return 0;
}

var resumed = await host.ResumeFileAsync(
    workflowPath,
    new ResumeRequest
    {
        SignalType = resumeSignal,
        Payload = resumePayload
    }).ConfigureAwait(false);

Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
Console.WriteLine($"Events file: {eventPath}");
return resumed.Success ? 0 : 1;

static Dictionary<string, string?> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            if (!options.ContainsKey("workflow"))
            {
                options["workflow"] = arg;
            }

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

static string ResolvePath(string? value, string defaultPath, string repoRoot)
{
    var candidate = string.IsNullOrWhiteSpace(value) ? defaultPath : value;
    return Path.IsPathRooted(candidate) ? candidate : Path.GetFullPath(Path.Combine(repoRoot, candidate));
}

static Dictionary<string, object> ParsePayload(string? payloadJson, IReadOnlyDictionary<string, object> fallback)
{
    if (string.IsNullOrWhiteSpace(payloadJson))
    {
        return new Dictionary<string, object>(fallback, StringComparer.OrdinalIgnoreCase);
    }

    using var document = JsonDocument.Parse(payloadJson);
    if (document.RootElement.ValueKind != JsonValueKind.Object)
    {
        throw new InvalidOperationException("Resume payload JSON must be an object.");
    }

    var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    foreach (var property in document.RootElement.EnumerateObject())
    {
        result[property.Name] = ConvertJsonValue(property.Value)!;
    }

    return result;
}

static object? ConvertJsonValue(JsonElement element)
    => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject()
            .ToDictionary(static property => property.Name, static property => ConvertJsonValue(property.Value), StringComparer.OrdinalIgnoreCase),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToArray(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

static bool HasFlag(string[] args, params string[] flags)
    => args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));

static void PrintHelp(string repoRoot)
{
    Console.WriteLine("Procedo.Example.AdvancedObservability");
    Console.WriteLine();
    Console.WriteLine("Runs a workflow with console + JSONL event sinks and optional persisted resume.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.AdvancedObservability");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.AdvancedObservability -- --workflow examples/78_template_persisted_resume_observability_demo.yaml --resume-signal approve --resume-payload-json '{\"ticket\":\"CHG-780\"}'");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --workflow <path>             Workflow path. Defaults to examples/78_template_persisted_resume_observability_demo.yaml.");
    Console.WriteLine("  --state-dir <path>            Persisted state directory. Defaults to .procedo/example-advanced-observability.");
    Console.WriteLine("  --run-id <value>              Persisted run id. Defaults to advanced-observability-demo.");
    Console.WriteLine("  --resume-signal <value>       Resume signal for waiting workflows. Defaults to approve for the default workflow.");
    Console.WriteLine("  --resume-payload-json <json>  Resume payload object as JSON.");
    Console.WriteLine("  --keep-state                  Reuse the state directory instead of cleaning it first.");
    Console.WriteLine();
    Console.WriteLine($"Repo root: {repoRoot}");
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
