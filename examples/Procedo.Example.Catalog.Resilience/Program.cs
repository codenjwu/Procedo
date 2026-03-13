using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

var expectedRuntimeFailure = new[]
{
    "10_timeout_failure.yaml",
    "11_continue_on_error_false.yaml",
    "12_continue_on_error_true.yaml",
    "21_cancellation_demo.yaml"
};

var expectedSuccess = new[]
{
    "09_retry_transient.yaml",
    "16_persistence_resume_happy_path.yaml"
};

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .Build();

var hasMismatch = false;

foreach (var scenario in expectedSuccess)
{
    var yamlPath = Path.Combine(repoRoot, "examples", scenario);
    var yaml = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(false);

    try
    {
        var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);
        Console.WriteLine($"{scenario} => {(result.Success ? "SUCCESS" : "FAILED")} (expected SUCCESS)");
        if (!result.Success)
        {
            Console.WriteLine($"  [{result.ErrorCode}] {result.Error}");
            hasMismatch = true;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{scenario} => EXCEPTION (expected SUCCESS)");
        Console.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
        hasMismatch = true;
    }
}

foreach (var scenario in expectedRuntimeFailure)
{
    var yamlPath = Path.Combine(repoRoot, "examples", scenario);
    var yaml = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(false);

    try
    {
        var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);
        Console.WriteLine($"{scenario} => {(result.Success ? "SUCCESS" : "FAILED")} (expected FAILED)");
        if (result.Success)
        {
            hasMismatch = true;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{scenario} => EXCEPTION (acceptable for failure demo): {ex.GetType().Name}");
    }
}

return hasMismatch ? 1 : 0;

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
