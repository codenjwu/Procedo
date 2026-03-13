using Microsoft.Extensions.DependencyInjection;
using Procedo.DSL;
using Procedo.Engine.Hosting;
using Procedo.Extensions.DependencyInjection;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;
using Procedo.Validation;
using Procedo.Validation.Models;

namespace Procedo.ContractTests;

public sealed class PublicPackageCrossTargetContractTests
{
    private const string HelloWorkflowYaml = """
name: hello_contract
version: 1
stages:
- stage: demo
  jobs:
  - job: main
    steps:
    - step: greet
      type: system.echo
      with:
        message: \"hello from contract\"
""";

    [Fact]
    public async Task HostBuilder_With_SystemPlugin_Should_Execute_Yaml_Across_Targets()
    {
        var host = new ProcedoHostBuilder()
            .ConfigurePlugins(registry => registry.AddSystemPlugin())
            .Build();

        var result = await host.ExecuteYamlAsync(HelloWorkflowYaml);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.RunId));
    }

    [Fact]
    public void Validator_With_SystemPlugin_Should_Report_No_Errors_For_Valid_Workflow()
    {
        var workflow = new YamlWorkflowParser().Parse(HelloWorkflowYaml);
        var registry = new PluginRegistry();
        registry.AddSystemPlugin();

        var result = new ProcedoWorkflowValidator().Validate(workflow, registry, ValidationOptions.Strict);

        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task DependencyInjection_Builder_Should_Resolve_Host_And_Run_Custom_Step_Across_Targets()
    {
        var services = new ServiceCollection();

        services.AddProcedo()
            .ConfigurePlugins(registry => registry.AddSystemPlugin())
            .RegisterStep("custom.inline", _ => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["message"] = "ok"
                }
            }));

        using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<ProcedoHost>();

        const string yaml = """
name: custom_contract
version: 1
stages:
- stage: demo
  jobs:
  - job: main
    steps:
    - step: run_custom
      type: custom.inline
""";

        var result = await host.ExecuteYamlAsync(yaml);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.RunId));
    }
}
