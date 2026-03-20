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

public sealed class WorkflowScenarioGoldenTests
{
    [Fact]
    public async Task Example_84_EtlReconciliationAuditDemo_Should_Create_Mismatch_Bundle_And_Hash()
    {
        var workflow = LoadWorkflow("84_etl_reconciliation_audit_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-etl-reconciliation");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("staging_dir", Path.Combine(tempRoot, "staging")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "write_delta_report");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "write_match_report");
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "etl-reconciliation-bundle.zip")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "etl-reconciliation-bundle.sha256.txt")));

            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRoot, "staging", "reconciliation-summary.json")));
            var root = json.RootElement;
            Assert.Equal("batch-20260319", root.GetProperty("batch").GetProperty("id").GetString());
            Assert.Equal(128, root.GetProperty("counts").GetProperty("source").GetInt32());
            Assert.Equal(125, root.GetProperty("counts").GetProperty("target").GetInt32());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_85_ComplianceAuditBundleDemo_Should_Create_NoException_Audit_Bundle()
    {
        var workflow = LoadWorkflow("85_compliance_audit_bundle_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-compliance-audit");
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
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "write_no_exception_note");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "write_exception_ticket");
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "compliance-audit-bundle.zip")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "compliance-audit-bundle.sha256.txt")));

            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempRoot, "evidence", "audit.json")));
            var root = json.RootElement;
            Assert.Equal("audit-2026-q1", root.GetProperty("audit").GetProperty("id").GetString());
            Assert.Equal("access-control", root.GetProperty("audit").GetProperty("control_family").GetString());
        }
        finally
        {
            TryDelete(tempRoot);
        }
    }

    [Fact]
    public async Task Example_86_ModelPromotionGovernanceDemo_Should_Wait_Resume_And_Create_Promotion_Bundle()
    {
        var workflow = LoadWorkflow("86_model_promotion_governance_demo.yaml");
        var tempRoot = CreateTempDirectory("procedo-model-promotion");
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("review_dir", Path.Combine(tempRoot, "review")),
            ("rollout_dir", Path.Combine(tempRoot, "rollout")),
            ("output_dir", Path.Combine(tempRoot, "output")));
        var store = new FileRunStateStore(Path.Combine(tempRoot, "state"));

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var first = await engine.ExecuteWithPersistenceAsync(workflow, CreateRegistry(), new NullLogger(), store, "model-promotion");

            Assert.False(first.Success);
            Assert.True(first.Waiting);

            var resumed = await engine.ResumeAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "model-promotion",
                new ResumeRequest { SignalType = "approve" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "model-promotion-bundle.zip")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "output", "model-promotion-bundle.sha256.txt")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "rollout", "westus.txt")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "rollout", "centralus.txt")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "rollout", "eastus-gated.txt")));

            var final = await store.GetRunAsync("model-promotion");
            Assert.NotNull(final);
            Assert.Equal(RunStatus.Completed, final!.Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["assess/governance/wait_for_governance_approval"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["rollout/promote/write_westus_note"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["rollout/promote/write_centralus_note"].Status);
            Assert.Equal(StepRunStatus.Skipped, final.Steps["rollout/promote/promote_eastus"].Status);
            Assert.Equal(StepRunStatus.Skipped, final.Steps["rollout/promote/write_eastus_note"].Status);
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
