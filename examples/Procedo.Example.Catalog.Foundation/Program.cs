using Procedo.Engine.Hosting;
using Procedo.Plugin.Demo;
using Procedo.Plugin.System;

var repoRoot = FindRepoRoot(AppContext.BaseDirectory);

var scenarios = new[]
{
    "hello_pipeline.yaml",
    "01_hello_echo.yaml",
    "02_linear_depends_on.yaml",
    "03_fan_out_fan_in.yaml",
    "04_multi_stage_multi_job.yaml",
    "05_outputs_and_expressions.yaml",
    "06_vars_expression_via_step.yaml",
    "07_job_max_parallelism.yaml",
    "08_workflow_job_parallel_override.yaml",
    "18_observability_console_events.yaml",
    "20_config_precedence_demo.yaml",
    "22_contract_smoke.yaml",
    "31_system_toolbox_demo.yaml",
    "32_system_file_ops_demo.yaml",
    "34_system_encoding_hash_demo.yaml",
    "35_system_archive_demo.yaml",
    "36_system_directory_demo.yaml",
    "37_system_json_demo.yaml",
    "38_system_csv_demo.yaml",
    "39_system_xml_demo.yaml"
};

var host = new ProcedoHostBuilder()
    .ConfigurePlugins(static registry =>
    {
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
    })
    .Build();

var hasMismatch = false;
foreach (var scenario in scenarios)
{
    var yamlPath = Path.Combine(repoRoot, "examples", scenario);
    var yaml = await File.ReadAllTextAsync(yamlPath).ConfigureAwait(false);

    try
    {
        var result = await host.ExecuteYamlAsync(yaml).ConfigureAwait(false);
        var ok = result.Success;
        Console.WriteLine($"{scenario} => {(ok ? "SUCCESS" : "FAILED")} (expected SUCCESS)");

        if (!ok)
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
