using System.Text.Json;
using Procedo.Core.Runtime;
using Procedo.Engine.Hosting;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
if (HasFlag(args, "--help", "-h"))
{
    PrintHelp(repoRoot);
    return 0;
}

var options = ParseOptions(args);
var defaultWorkflowPath = Path.Combine(repoRoot, "examples", "71_callback_resume_identity_demo.yaml");
var workflowPath = ResolvePath(GetOption(options, "workflow"), defaultWorkflowPath, repoRoot);
var isDefaultWorkflow = string.Equals(workflowPath, defaultWorkflowPath, StringComparison.OrdinalIgnoreCase);

var waitType = GetOption(options, "wait-type") ?? "signal";
var waitKey = GetOption(options, "wait-key") ?? (isDefaultWorkflow ? "callback-identity-demo" : null);
var expectedSignalType = GetOption(options, "expected-signal") ?? (isDefaultWorkflow ? "approve" : null);
var signalType = GetOption(options, "signal-type") ?? expectedSignalType ?? (isDefaultWorkflow ? "approve" : null);
var matchBehavior = ParseMatchBehavior(GetOption(options, "match-behavior"));
var payload = ParsePayload(GetOption(options, "payload-json"), new Dictionary<string, object>
{
    ["approved_by"] = "callback-host",
    ["ticket"] = "CHG-710"
});

if (string.IsNullOrWhiteSpace(waitKey) || string.IsNullOrWhiteSpace(signalType))
{
    Console.Error.WriteLine("Callback resume requires --wait-key and --signal-type for non-default workflows.");
    Console.Error.WriteLine("Use --help for usage.");
    return 1;
}

var defaultStateDirectory = Path.Combine(repoRoot, ".procedo", "example-callback-resume-host");
var stateDirectory = ResolvePath(GetOption(options, "state-dir"), defaultStateDirectory, repoRoot);
if (!HasFlag(args, "--keep-state"))
{
    TryDeleteDirectory(stateDirectory);
}

Directory.CreateDirectory(stateDirectory);

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry => registry.AddSystemPlugin())
    .UseLocalRunStateStore(stateDirectory)
    .Build();

var first = await host.ExecuteFileAsync(workflowPath).ConfigureAwait(false);
Console.WriteLine($"First execution: success={first.Success}, waiting={first.Waiting}, code={first.ErrorCode}");
if (!first.Waiting)
{
    return first.Success ? 0 : 1;
}

var waiting = await host.FindWaitingRunsAsync(new WaitingRunQuery
{
    WaitType = waitType,
    WaitKey = waitKey,
    ExpectedSignalType = expectedSignalType
}).ConfigureAwait(false);

Console.WriteLine($"Waiting matches: {waiting.Count}");
foreach (var wait in waiting)
{
    Console.WriteLine($"- run={wait.RunId} step={wait.StepPath} signal={wait.ExpectedSignalType}");
}

var resumed = await host.ResumeWaitingRunAsync(new ResumeWaitingRunRequest
{
    WaitType = waitType,
    WaitKey = waitKey,
    ExpectedSignalType = expectedSignalType,
    SignalType = signalType,
    Payload = payload,
    MatchBehavior = matchBehavior
}).ConfigureAwait(false);

Console.WriteLine($"Resume execution: success={resumed.Success}, waiting={resumed.Waiting}, code={resumed.ErrorCode}");
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

static WaitingRunMatchBehavior ParseMatchBehavior(string? value)
{
    if (!string.IsNullOrWhiteSpace(value)
        && Enum.TryParse<WaitingRunMatchBehavior>(value, ignoreCase: true, out var parsed))
    {
        return parsed;
    }

    return WaitingRunMatchBehavior.FailWhenMultiple;
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
        throw new InvalidOperationException("Payload JSON must be an object.");
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
    Console.WriteLine("Procedo.Example.CallbackResumeHost");
    Console.WriteLine();
    Console.WriteLine("Runs a persisted workflow, lists matching waits, and resumes one wait by identity.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.CallbackResumeHost");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.CallbackResumeHost -- --workflow examples/71_callback_resume_identity_demo.yaml --wait-key callback-identity-demo --expected-signal approve --signal-type approve --payload-json '{\"approved_by\":\"ops-bot\"}'");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --workflow <path>          Workflow path. Defaults to examples/71_callback_resume_identity_demo.yaml.");
    Console.WriteLine("  --state-dir <path>         Persisted state directory. Defaults to .procedo/example-callback-resume-host.");
    Console.WriteLine("  --wait-type <value>        Wait type query/resume filter. Defaults to signal.");
    Console.WriteLine("  --wait-key <value>         Wait key filter. Required for non-default workflows.");
    Console.WriteLine("  --expected-signal <value>  Expected signal filter. Defaults to approve for the default workflow.");
    Console.WriteLine("  --signal-type <value>      Signal type sent on resume. Defaults to the expected signal.");
    Console.WriteLine("  --payload-json <json>      Resume payload object as JSON.");
    Console.WriteLine("  --match-behavior <value>   FailWhenMultiple, ResumeNewest, or ResumeOldest.");
    Console.WriteLine("  --keep-state               Reuse the state directory instead of cleaning it first.");
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
