using System.Text.Json;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.DSL;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public sealed class WorkflowControlFlowMatrixTests
{
    [Fact]
    public async Task Example_74_ControlFlowArrayIterationDemo_Should_Expand_Array_Items_And_Apply_Runtime_Gating()
    {
        var workflow = LoadWorkflow("74_control_flow_array_iteration_demo.yaml");
        var sink = new InMemorySink();

        var result = await ExecuteAsync(workflow, sink);

        Assert.True(result.Success, result.Error);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_qa");
        Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_prod");
        Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_other");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "announce_eastus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "announce_westus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "announce_centralus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "deploy_eastus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_westus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_centralus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "final_summary");
    }

    [Fact]
    public async Task Example_75_MixedTemplateRuntimeControlFlowDemo_Should_Compose_Template_Branching_Runtime_Gating_And_Structured_Metadata()
    {
        var workflow = LoadTemplatedWorkflow("75_mixed_template_runtime_control_flow_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-control-flow-mix");
        var outputFile = Path.Combine(tempRoot, "control-flow-summary.txt");
        workflow.Variables["workspace"] = tempRoot;
        workflow.Variables["output_file"] = outputFile;
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_prod");
            Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_qa");
            Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_other");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "gate_eastus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "gate_westus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "gate_centralus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "hotfix_eastus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "hotfix_westus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "hotfix_centralus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "final_summary");
            Assert.True(File.Exists(outputFile));
            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var resultRoot = json.RootElement.GetProperty("result");
            Assert.Equal("catalog-api-prod", resultRoot.GetProperty("release_label").GetString());
            var metadata = resultRoot.GetProperty("metadata");
            Assert.Equal("commerce", metadata.GetProperty("team").GetString());
            Assert.Equal("release-ops", metadata.GetProperty("owner").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public void Example_76_EachObjectIterationValidationError_Should_Fail_Clearly()
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", "76_each_object_iteration_validation_error.yaml");

        var ex = Assert.Throws<InvalidOperationException>(() => new WorkflowTemplateLoader().LoadFromFile(path));

        Assert.Contains("must evaluate to an array", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkflowDefinition LoadWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        var yaml = File.ReadAllText(path);
        return new YamlWorkflowParser().Parse(yaml);
    }

    private static WorkflowDefinition LoadTemplatedWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        return new WorkflowTemplateLoader().LoadFromFile(path);
    }

    private static async Task<WorkflowRunResult> ExecuteAsync(WorkflowDefinition workflow, InMemorySink sink)
        => await new ProcedoWorkflowEngine().ExecuteAsync(workflow, CreateRegistry(), new NullLogger(), sink);

    private static IPluginRegistry CreateRegistry()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
        return registry;
    }

    private static string CreateTempDirectory(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, true); } catch { }
    }

    private sealed class InMemorySink : IExecutionEventSink
    {
        public List<ExecutionEvent> Events { get; } = new();

        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
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
