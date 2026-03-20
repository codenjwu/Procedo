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

public sealed class WorkflowScenarioPackIntegrationTests
{
    [Fact]
    public async Task Example_80_ReleaseTrainCanaryApproval_Should_Wait_Resume_And_Create_Release_Bundle()
    {
        var workflow = LoadWorkflow("80_release_train_canary_approval.yaml");
        var tempRoot = CreateTempDirectory("procedo-release-train-canary");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("request_dir", Path.Combine(tempRoot, "request")),
            ("rollout_dir", Path.Combine(tempRoot, "rollout")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var store = new FileRunStateStore(Path.Combine(tempRoot, "state"));

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var first = await engine.ExecuteWithPersistenceAsync(workflow, CreateRegistry(), new NullLogger(), store, "release-train-canary");

            Assert.False(first.Success);
            Assert.True(first.Waiting);

            var resumed = await engine.ResumeAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "release-train-canary",
                new ResumeRequest { SignalType = "approve" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "release-train-bundle.zip")));

            var final = await store.GetRunAsync("release-train-canary");
            Assert.NotNull(final);
            Assert.Equal(RunStatus.Completed, final!.Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_flow/wait_for_canary_approval"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_flow/deploy_westus"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_flow/deploy_centralus"].Status);
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_81_ReleaseTrainRecoveryDemo_Should_Package_Rollback_Path()
    {
        var workflow = LoadWorkflow("81_release_train_recovery_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-release-train-recovery");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("recovery_dir", Path.Combine(tempRoot, "recovery")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "announce_failed_canary");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "approved_path_notice");
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "rollback-bundle.zip")));

            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRoot, "recovery", "rollback-manifest.json")));
            var root = json.RootElement.GetProperty("result");
            Assert.Equal("billing-api", root.GetProperty("service").GetString());
            Assert.Equal("rejected", root.GetProperty("canary_verdict").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_82_IncidentTriageSeverityBranching_Should_Branch_And_Create_Incident_Bundle()
    {
        var workflow = LoadWorkflow("82_incident_triage_severity_branching.yaml");
        var tempRoot = CreateTempDirectory("procedo-incident-triage");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("evidence_dir", Path.Combine(tempRoot, "evidence")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "route_sev1");
            Assert.DoesNotContain(sink.Events, e => e.StepId == "route_sev2");
            Assert.DoesNotContain(sink.Events, e => e.StepId == "route_other");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "write_sev1_containment");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "write_standard_triage");
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "incident-triage-bundle.zip")));
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_83_MaintenanceWindowRunbookDemo_Should_Wait_Resume_And_Create_Runbook_Bundle()
    {
        var workflow = LoadWorkflow("83_maintenance_window_runbook_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-maintenance-window");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("request_dir", Path.Combine(tempRoot, "request")),
            ("runbook_dir", Path.Combine(tempRoot, "runbook")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var store = new FileRunStateStore(Path.Combine(tempRoot, "state"));

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var first = await engine.ExecuteWithPersistenceAsync(workflow, CreateRegistry(), new NullLogger(), store, "maintenance-window");

            Assert.False(first.Success);
            Assert.True(first.Waiting);

            var resumed = await engine.ResumeAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "maintenance-window",
                new ResumeRequest { SignalType = "start" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "maintenance-runbook.zip")));

            var final = await store.GetRunAsync("maintenance-window");
            Assert.NotNull(final);
            Assert.Equal(RunStatus.Completed, final!.Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["maintenance/runbook/wait_for_start"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["maintenance/runbook/package_runbook"].Status);
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    private static WorkflowDefinition LoadWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        return new YamlWorkflowParser().Parse(File.ReadAllText(path));
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

    private static void OverrideWorkspace(WorkflowDefinition workflow, params (string Key, string Value)[] values)
    {
        foreach (var (key, value) in values)
        {
            workflow.Variables[key] = value;
        }
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
