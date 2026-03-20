using System.Text.Json;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Persistence.Stores;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public sealed class WorkflowCompositionGoldenTests
{
    [Fact]
    public async Task Example_77_TemplateNullConditionAuditDemo_Should_Compose_Template_Null_Overrides_And_Runtime_Gating()
    {
        var workflow = LoadTemplatedWorkflow("77_template_null_condition_audit_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-composition-audit");
        var outputFile = Path.Combine(tempRoot, "audit-summary.json");
        workflow.Variables["workspace"] = tempRoot;
        workflow.Variables["output_file"] = outputFile;
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_qa");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "deploy_eastus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_westus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_centralus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "write_summary_with_note");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "write_summary_without_note");
            Assert.True(File.Exists(outputFile));

            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var root = json.RootElement.GetProperty("result");
            Assert.Equal("inventory-api-qa", root.GetProperty("release_label").GetString());
            Assert.Equal("standard", root.GetProperty("release_channel").GetString());
            var metadata = root.GetProperty("metadata");
            Assert.Equal("platform", metadata.GetProperty("team").GetString());
            Assert.Equal(JsonValueKind.Null, metadata.GetProperty("owner").ValueKind);
            Assert.Equal("core", metadata.GetProperty("tier").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_78_TemplatePersistedResumeObservabilityDemo_Should_Wait_Resume_And_Write_Golden_Summary()
    {
        var workflow = LoadTemplatedWorkflow("78_template_persisted_resume_observability_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-composition-resume");
        var requestDir = Path.Combine(tempRoot, "request");
        var outputDir = Path.Combine(tempRoot, "output");
        workflow.Variables["workspace"] = tempRoot;
        workflow.Variables["request_dir"] = requestDir;
        workflow.Variables["output_dir"] = outputDir;
        var storeRoot = Path.Combine(tempRoot, "state");
        Directory.CreateDirectory(storeRoot);
        var sink = new InMemorySink();

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(storeRoot);

            var first = await engine.ExecuteWithPersistenceAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "composition-resume",
                sink);

            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var resumed = await engine.ResumeAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "composition-resume",
                new ResumeRequest
                {
                    SignalType = "approve",
                    Payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ticket"] = "CHG-780",
                        ["approved_by"] = "ops-bot"
                    }
                },
                sink);

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "resume-summary.json")));
            Assert.True(File.Exists(Path.Combine(requestDir, "approval-request.txt")));

            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputDir, "resume-summary.json")));
            var root = json.RootElement.GetProperty("result");
            Assert.Equal("approve", root.GetProperty("signal_type").GetString());
            var payload = root.GetProperty("payload");
            Assert.Equal("CHG-780", payload.GetProperty("ticket").GetString());
            Assert.Equal("ops-bot", payload.GetProperty("approved_by").GetString());
            var metadata = root.GetProperty("metadata");
            Assert.Equal("commerce", metadata.GetProperty("team").GetString());
            Assert.Equal("release-ops", metadata.GetProperty("owner").GetString());

            var runState = await store.GetRunAsync("composition-resume");
            Assert.NotNull(runState);
            Assert.Equal(RunStatus.Completed, runState!.Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["release/approval/wait_for_approval"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["release/approval/write_summary"].Status);
            Assert.Equal(StepRunStatus.Skipped, runState.Steps["release/rollout/gate_eastus"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["release/rollout/gate_westus"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["release/rollout/gate_centralus"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["release/rollout/smoke_centralus"].Status);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunWaiting);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunResumed);
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_79_TemplateArtifactBundleCompositionDemo_Should_Create_Golden_Artifacts_And_Hash()
    {
        var workflow = LoadTemplatedWorkflow("79_template_artifact_bundle_composition_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-composition-bundle");
        var packageDir = Path.Combine(tempRoot, "package");
        var outputDir = Path.Combine(tempRoot, "output");
        workflow.Variables["workspace"] = tempRoot;
        workflow.Variables["package_dir"] = packageDir;
        workflow.Variables["output_dir"] = outputDir;
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_prod");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "note_eastus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "note_westus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "note_centralus");
            Assert.True(File.Exists(Path.Combine(packageDir, "manifest.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "composition-bundle.zip")));
            Assert.True(File.Exists(Path.Combine(outputDir, "composition-bundle.sha256.txt")));

            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(packageDir, "manifest.json")));
            var manifest = json.RootElement.GetProperty("manifest");
            Assert.Equal("identity-api-prod", manifest.GetProperty("release_label").GetString());
            var metadata = manifest.GetProperty("metadata");
            Assert.Equal("security", metadata.GetProperty("team").GetString());
            Assert.Equal(JsonValueKind.Null, metadata.GetProperty("owner").ValueKind);
            Assert.Equal("primary", metadata.GetProperty("ring").GetString());

            var hashText = File.ReadAllText(Path.Combine(outputDir, "composition-bundle.sha256.txt")).Trim();
            Assert.False(string.IsNullOrWhiteSpace(hashText));
        }
        finally
        {
            TryDelete(tempRoot);
        }
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
