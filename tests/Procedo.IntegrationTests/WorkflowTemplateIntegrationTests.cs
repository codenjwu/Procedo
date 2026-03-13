using Procedo.Core.Models;
using Procedo.Engine.Hosting;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public class WorkflowTemplateIntegrationTests
{
    [Fact]
    public async Task ExecuteFileAsync_Should_Run_Template_With_Runtime_Parameter_Overrides()
    {
        var host = new ProcedoHostBuilder()
            .ConfigurePlugins(static registry => registry.AddSystemPlugin())
            .Build();

        var workflowPath = Path.Combine(GetRepoRoot(), "examples", "48_template_parameters_demo.yaml");
        var result = await host.ExecuteFileAsync(
            workflowPath,
            new Dictionary<string, object>
            {
                ["environment"] = "prod",
                ["region"] = "westus"
            }).ConfigureAwait(false);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task RuntimeStyle_Loaded_Template_Should_Resolve_Variables_In_System_Echo_Input()
    {
        var workflowPath = Path.Combine(GetRepoRoot(), "examples", "48_template_parameters_demo.yaml");
        var workflow = new Procedo.DSL.WorkflowTemplateLoader().LoadFromFile(
            workflowPath,
            new Dictionary<string, object>
            {
                ["environment"] = "prod",
                ["region"] = "westus"
            });

        var sink = new InMemorySink();
        IPluginRegistry registry = new PluginRegistry();
        registry.AddSystemPlugin();

        var result = await new Procedo.Engine.ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink).ConfigureAwait(false);

        Assert.True(result.Success, result.Error);
        var completed = Assert.Single(sink.Events.Where(e => e.EventType == Procedo.Observability.ExecutionEventType.StepCompleted && e.StepId == "announce"));
        var outputs = Assert.IsType<Dictionary<string, object>>(completed.Outputs);
        Assert.Equal("Building custom-procedo-prod", outputs["message"]?.ToString());
    }

    [Fact]
    public async Task ExecuteAsync_Should_Return_Template_SourcePath_On_Runtime_Failure()
    {
        var root = CreateTempDirectory();
        try
        {
            var templatePath = Path.Combine(root, "base.yaml");
            File.WriteAllText(templatePath, "name: base\nversion: 1\nstages:\n- stage: build\n  jobs:\n  - job: package\n    steps:\n    - step: announce\n      type: missing.plugin\n");

            var childPath = Path.Combine(root, "child.yaml");
            File.WriteAllText(childPath, "template: ./base.yaml\nname: child\n");

            var workflow = new Procedo.DSL.WorkflowTemplateLoader().LoadFromFile(childPath);
            IPluginRegistry registry = new PluginRegistry();
            var sink = new InMemorySink();

            var result = await new Procedo.Engine.ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink).ConfigureAwait(false);

            Assert.False(result.Success);
            Assert.Equal(templatePath, result.SourcePath);
            var stepFailed = Assert.Single(sink.Events.Where(e => e.EventType == Procedo.Observability.ExecutionEventType.StepFailed));
            Assert.Equal(templatePath, stepFailed.SourcePath);
            var failed = Assert.Single(sink.Events.Where(e => e.EventType == Procedo.Observability.ExecutionEventType.RunFailed));
            Assert.Equal(templatePath, failed.SourcePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task ExecuteFileAsync_Should_Reject_Runtime_Parameter_Overrides_That_Violate_Richer_Schema()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflowPath = Path.Combine(root, "parameter_schema.yaml");
            File.WriteAllText(workflowPath,
                """
                name: parameter_schema_demo
                version: 1
                parameters:
                  environment:
                    type: string
                    allowed_values:
                    - dev
                    - prod
                  retry_count:
                    type: int
                    min: 1
                    max: 5
                stages:
                - stage: validate
                  jobs:
                  - job: schema
                    steps:
                    - step: announce
                      type: system.echo
                      with:
                        message: "env=${params.environment} retry=${params.retry_count}"
                """);

            var host = new ProcedoHostBuilder()
                .ConfigurePlugins(static registry => registry.AddSystemPlugin())
                .Build();

            var ex = await Assert.ThrowsAsync<ProcedoValidationException>(() => host.ExecuteFileAsync(
                workflowPath,
                new Dictionary<string, object>
                {
                    ["environment"] = "qa",
                    ["retry_count"] = 9
                })).ConfigureAwait(false);

            Assert.Contains(ex.ValidationResult.Issues, issue => issue.Code == "PV014" && issue.Path == "parameters.environment");
            Assert.Contains(ex.ValidationResult.Issues, issue => issue.Code == "PV014" && issue.Path == "parameters.retry_count");
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-template-integration-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class InMemorySink : Procedo.Observability.IExecutionEventSink
    {
        public List<Procedo.Observability.ExecutionEvent> Events { get; } = new();

        public Task WriteAsync(Procedo.Observability.ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(executionEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
