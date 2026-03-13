using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

var expectedSuccess = new[]
{
    "23_large_dag_stress.yaml",
    "24_end_to_end_reference.yaml",
    "25_data_platform_full_pipeline.yaml",
    "27_multi_source_etl_reconciliation.yaml",
    "28_ml_feature_pipeline.yaml"
};

var expectedRuntimeFailure = new[]
{
    "26_branched_release_train.yaml",
    "29_finops_daily_close.yaml"
};

var expectedValidationFailure = new[]
{
    "30_enterprise_reference_pipeline.yaml"
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

foreach (var scenario in expectedValidationFailure)
{
    var yamlPath = Path.Combine(repoRoot, "examples", scenario);
    var yaml = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(false);

    try
    {
        var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);
        Console.WriteLine($"{scenario} => {(result.Success ? "SUCCESS" : "FAILED")} (expected VALIDATION FAILURE)");
        hasMismatch = true;
    }
    catch (ProcedoValidationException ex)
    {
        Console.WriteLine($"{scenario} => VALIDATION FAILURE (expected)");
        Console.WriteLine($"  {ex.ValidationResult.Issues.Count} issue(s)");
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
