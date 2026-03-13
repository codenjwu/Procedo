using System.Diagnostics;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
var entries = BuildEntries(repoRoot);

if (args.Length == 0 || HasFlag(args, "--help", "-h"))
{
    PrintHelp(entries);
    return 0;
}

if (HasFlag(args, "--list"))
{
    PrintEntries(entries);
    return 0;
}

if (!TryGetRunTarget(args, out var target))
{
    Console.Error.WriteLine("Specify --list or --run <name>.");
    return 1;
}

var selected = ResolveTargets(target, entries);
if (selected.Count == 0)
{
    Console.Error.WriteLine($"Unknown example target '{target}'. Use --list to see available targets.");
    return 1;
}

var hasFailures = false;
foreach (var entry in selected)
{
    Console.WriteLine($">>> {entry.Key} [{entry.Kind}]");
    Console.WriteLine($"    {entry.Description}");

    var exitCode = await RunProjectAsync(entry.ProjectPath).ConfigureAwait(false);
    Console.WriteLine($"<<< {entry.Key} => {(exitCode == 0 ? "SUCCESS" : $"FAILED ({exitCode})")}");
    Console.WriteLine();

    if (exitCode != 0)
    {
        hasFailures = true;
    }
}

return hasFailures ? 1 : 0;

static IReadOnlyList<ExampleEntry> BuildEntries(string repoRoot)
{
    var examplesRoot = Path.Combine(repoRoot, "examples");

    return new List<ExampleEntry>
    {
        Project("basic", "Direct parser + validation + engine usage.", "Procedo.Example.Basic"),
        Project("custom-steps", "Delegate, DI-backed, and method-binding custom steps.", "Procedo.Example.CustomSteps"),
        Project("control-flow", "Focused control-flow examples for runtime conditions and branching.", "Procedo.Example.ControlFlow"),
        Project("dependency-injection", "Microsoft.Extensions.DependencyInjection integration.", "Procedo.Example.DependencyInjection"),
        Project("extensible", "ProcedoHostBuilder extension pattern for reusable host setup.", "Procedo.Example.Extensible"),
        Project("multi-stage-promotion", "Template-driven multi-stage promotion with approval resume.", "Procedo.Example.MultiStagePromotion"),
        Project("observability", "Console + JSONL observability sinks.", "Procedo.Example.Observability"),
        Project("persistence-resume", "Local persistence and resume walkthrough.", "Procedo.Example.PersistenceResume"),
        Project("scenario-pack", "Curated simple-to-enterprise workflow run pack.", "Procedo.Example.ScenarioPack"),
        Project("secure-runtime", "Locked-down host with system plugin security options.", "Procedo.Example.SecureRuntime"),
        Project("template-release-pack", "Template-driven branching, gating, and release bundle packaging.", "Procedo.Example.TemplateReleasePack"),
        Project("templates", "Template-driven workflow execution.", "Procedo.Example.Templates"),
        Project("template-wait-resume", "Template-driven approval pause and resume walkthrough.", "Procedo.Example.TemplateWaitResume"),
        Project("validation", "Validation-only walkthrough over valid and invalid workflows.", "Procedo.Example.Validation"),
        Project("wait-resume", "Generic wait -> resume signal walkthrough.", "Procedo.Example.WaitResume"),
        Project("wait-resume-observability", "Wait/resume with structured observability traces.", "Procedo.Example.WaitResumeObservability"),
        Catalog("foundation", "Catalog of foundational YAML scenarios expected to succeed.", "Procedo.Example.Catalog.Foundation"),
        Catalog("resilience", "Catalog of retry/timeout/cancel/failure YAML scenarios.", "Procedo.Example.Catalog.Resilience"),
        Catalog("enterprise", "Catalog of advanced enterprise YAML scenarios.", "Procedo.Example.Catalog.Enterprise")
    };

    ExampleEntry Project(string key, string description, string projectName) =>
        new(
            key,
            "project",
            description,
            Path.Combine(examplesRoot, projectName, $"{projectName}.csproj"));

    ExampleEntry Catalog(string key, string description, string projectName) =>
        new(
            key,
            "catalog",
            description,
            Path.Combine(examplesRoot, projectName, $"{projectName}.csproj"));
}

static List<ExampleEntry> ResolveTargets(string target, IReadOnlyList<ExampleEntry> entries)
{
    return target.ToLowerInvariant() switch
    {
        "all" => entries.ToList(),
        "catalogs" => entries.Where(static entry => entry.Kind == "catalog").ToList(),
        "projects" => entries.Where(static entry => entry.Kind == "project").ToList(),
        _ => entries.Where(entry => string.Equals(entry.Key, target, StringComparison.OrdinalIgnoreCase)).ToList()
    };
}

static async Task<int> RunProjectAsync(string projectPath)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        RedirectStandardOutput = false,
        RedirectStandardError = false,
        UseShellExecute = false
    };

    startInfo.ArgumentList.Add("run");
    startInfo.ArgumentList.Add("--project");
    startInfo.ArgumentList.Add(projectPath);

    using var process = Process.Start(startInfo);
    if (process is null)
    {
        throw new InvalidOperationException($"Could not start dotnet for '{projectPath}'.");
    }

    await process.WaitForExitAsync().ConfigureAwait(false);
    return process.ExitCode;
}

static bool HasFlag(string[] args, params string[] flags)
    => args.Any(arg => flags.Contains(arg, StringComparer.OrdinalIgnoreCase));

static bool TryGetRunTarget(string[] args, out string target)
{
    target = string.Empty;

    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], "--run", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            target = args[i + 1];
            return true;
        }
    }

    return false;
}

static void PrintEntries(IReadOnlyList<ExampleEntry> entries)
{
    Console.WriteLine("Available example targets:");
    foreach (var entry in entries.OrderBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase))
    {
        Console.WriteLine($"- {entry.Key} [{entry.Kind}]");
        Console.WriteLine($"  {entry.Description}");
    }

    Console.WriteLine();
    Console.WriteLine("Groups:");
    Console.WriteLine("- projects");
    Console.WriteLine("- catalogs");
    Console.WriteLine("- all");
}

static void PrintHelp(IReadOnlyList<ExampleEntry> entries)
{
    Console.WriteLine("Procedo Example Catalog");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.Catalog -- --list");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.Catalog -- --run basic");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.Catalog -- --run catalogs");
    Console.WriteLine("  dotnet run --project examples/Procedo.Example.Catalog -- --run all");
    Console.WriteLine();
    PrintEntries(entries);
}

static string FindRepoRoot(string startDirectory)
{
    var current = new DirectoryInfo(startDirectory);
    while (current is not null)
    {
        var slnPath = Path.Combine(current.FullName, "Procedo.sln");
        if (File.Exists(slnPath))
        {
            return current.FullName;
        }

        current = current.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate repository root (Procedo.sln).");
}

internal sealed record ExampleEntry(string Key, string Kind, string Description, string ProjectPath);
