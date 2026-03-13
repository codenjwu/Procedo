using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.DSL;
using Procedo.Engine;
using Procedo.Engine.Hosting;
using Procedo.Observability;
using Procedo.Persistence.Stores;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public class WorkflowDemoExamplesIntegrationTests
{
    [Fact]
    public async Task Example_09_RetryTransient_Should_Succeed()
    {
        var workflow = LoadWorkflow("09_retry_transient.yaml");

        var result = await ExecuteAsync(workflow);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Example_10_TimeoutFailure_Should_Fail()
    {
        var workflow = LoadWorkflow("10_timeout_failure.yaml");

        var result = await ExecuteAsync(workflow);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Example_12_ContinueOnErrorTrue_Should_Run_Sibling_And_Still_Fail_Run()
    {
        var workflow = LoadWorkflow("12_continue_on_error_true.yaml");
        var sink = new InMemorySink();

        var result = await ExecuteAsync(workflow, sink);

        Assert.False(result.Success);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepFailed && e.StepId == "bad");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "good");
    }

    [Fact]
    public async Task Example_17_PersistenceResumeAfterFailure_Should_Resume_And_Succeed()
    {
        var workflow = LoadWorkflow("17_persistence_resume_after_failure.yaml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-resume-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: null);
            Assert.False(first.Success);
            Assert.False(string.IsNullOrWhiteSpace(first.RunId));

            var resumed = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, first.RunId);
            Assert.True(resumed.Success);

            var final = await store.GetRunAsync(first.RunId!);
            Assert.NotNull(final);
            Assert.Equal(Procedo.Core.Runtime.RunStatus.Completed, final!.Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, final.Steps["recover/resume/ok_before"].Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, final.Steps["recover/resume/fail_once"].Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, final.Steps["recover/resume/after_recover"].Status);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_24_EndToEndReference_Should_Succeed()
    {
        var workflow = LoadWorkflow("24_end_to_end_reference.yaml");

        var result = await ExecuteAsync(workflow);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Example_51_ComprehensiveSystemBundle_Should_Succeed()
    {
        var workflow = LoadWorkflow("51_comprehensive_system_bundle_demo.yaml");

        var result = await ExecuteAsync(workflow);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task Example_50_ComprehensiveTemplateRelease_Should_Succeed()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", "50_comprehensive_template_release_demo.yaml");

        var host = new Procedo.Engine.Hosting.ProcedoHostBuilder()
            .ConfigurePlugins(static registry =>
            {
                registry.AddSystemPlugin();
                registry.AddDemoPlugin();
            })
            .Build();

        var result = await host.ExecuteFileAsync(path);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task Example_52_ComprehensiveWaitResumeBundle_Should_Resume_And_Succeed()
    {
        var workflow = LoadWorkflow("52_comprehensive_wait_resume_bundle_demo.yaml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-wait-bundle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        var bundleRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".procedo", "comprehensive-wait-bundle"));
        var inboundDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".procedo", "comprehensive-wait-bundle", "inbound"));
        var outputDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".procedo", "comprehensive-wait-bundle", "output"));

        try
        {
            try { Directory.Delete(bundleRoot, true); } catch { }

            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: "wait-bundle");
            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("file", first.WaitingType);

            Directory.CreateDirectory(inboundDir);
            await File.WriteAllTextAsync(Path.Combine(inboundDir, "approved.txt"), "approved-by=operator");

            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "wait-bundle",
                new ResumeRequest { SignalType = "check" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "approval-bundle.zip")));
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
            try { Directory.Delete(bundleRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_53_RuntimeConditionDemo_Should_Succeed_And_Emit_StepSkipped()
    {
        var workflow = LoadWorkflow("53_runtime_condition_demo.yaml");
        var sink = new InMemorySink();

        var result = await ExecuteAsync(workflow, sink);

        Assert.True(result.Success, result.Error);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "deploy_prod");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_non_prod");
    }

    [Fact]
    public async Task Example_54_TemplateRuntimeConditionDemo_Should_Succeed()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", "54_template_runtime_condition_demo.yaml");

        var host = new ProcedoHostBuilder()
            .ConfigurePlugins(static registry =>
            {
                registry.AddSystemPlugin();
                registry.AddDemoPlugin();
            })
            .Build();

        var result = await host.ExecuteFileAsync(path);

        Assert.True(result.Success, result.Error);
    }

    [Fact]
    public async Task Example_55_PersistenceConditionSkipDemo_Should_Persist_Skipped_Status()
    {
        var workflow = LoadWorkflow("55_persistence_condition_skip_demo.yaml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-skip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var result = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: null);

            Assert.True(result.Success, result.Error);
            var persisted = await store.GetRunAsync(result.RunId!);
            Assert.NotNull(persisted);
            Assert.Equal(StepRunStatus.Skipped, persisted!.Steps["recover/inspect/gated_prod_only"].Status);
            Assert.Equal(StepRunStatus.Completed, persisted.Steps["recover/inspect/continue_after_skip"].Status);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_56_ChangeWindowReleaseDemo_Should_Resume_And_Create_Release_Bundle()
    {
        var workflow = LoadWorkflow("56_change_window_release_demo.yaml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-change-window-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("request_dir", Path.Combine(tempRoot, "request")),
            ("handoff_dir", Path.Combine(tempRoot, "handoff")),
            ("output_dir", Path.Combine(tempRoot, "output")));

        var outputDir = Path.Combine(tempRoot, "output");

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: "change-window");
            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "change-window",
                new ResumeRequest { SignalType = "approve" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "release-bundle.zip")));

            var final = await store.GetRunAsync("change-window");
            Assert.NotNull(final);
            Assert.Equal(StepRunStatus.Completed, final!.Steps["release/approval_flow/package_release_bundle"].Status);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_57_IncidentEvidenceBundleDemo_Should_Succeed_And_Create_Expanded_Evidence()
    {
        var workflow = LoadWorkflow("57_incident_evidence_bundle_demo.yaml");
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-incident-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("inbound_dir", Path.Combine(tempRoot, "inbound")),
            ("output_dir", Path.Combine(tempRoot, "output")));

        var outputDir = Path.Combine(tempRoot, "output");

        try
        {
            var result = await ExecuteAsync(workflow);

            Assert.True(result.Success, result.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "incident-evidence.zip")));
            Assert.True(File.Exists(Path.Combine(outputDir, "expanded", "metadata.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "expanded", "timeline.csv")));
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_58_RuntimeExpressionFunctionShowcase_Should_Skip_Prod_Only_And_Complete_Runtime_Function_Steps()
    {
        var workflow = LoadWorkflow("58_runtime_expression_function_showcase.yaml");
        var sink = new InMemorySink();

        var result = await ExecuteAsync(workflow, sink);

        Assert.True(result.Success, result.Error);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "qa_or_prod_route");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "prod_only_gate");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "api_suffix_check");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "non_legacy_check");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "allowed_region_check");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "approved_channel_check");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "final_summary");
    }

    [Fact]
    public async Task Example_59_BranchingOperatorShowcase_Should_Expand_Qa_Branch_And_Apply_Runtime_Region_Gating()
    {
        var workflow = LoadWorkflow("59_branching_operator_showcase.yaml");
        var sink = new InMemorySink();

        var result = await ExecuteAsync(workflow, sink);

        Assert.True(result.Success, result.Error);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_qa");
        Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_prod");
        Assert.DoesNotContain(sink.Events, e => e.StepId == "branch_dev");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "deploy_eastus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_westus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_centralus");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "summary");
    }

    [Fact]
    public async Task Example_60_TemplateBranchingReleasePackDemo_Should_Combine_Template_Branching_Runtime_Gating_And_Artifacts()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", "60_template_branching_release_pack_demo.yaml");
        var workflow = new WorkflowTemplateLoader().LoadFromFile(path);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-template-branch-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("handoff_dir", Path.Combine(tempRoot, "handoff")),
            ("output_dir", Path.Combine(tempRoot, "output")));

        var outputDir = Path.Combine(tempRoot, "output");
        var sink = new InMemorySink();

        try
        {
            var result = await ExecuteAsync(workflow, sink);

            Assert.True(result.Success, result.Error);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "branch_prod");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "deploy_eastus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_westus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "deploy_centralus");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "announce_hotfix");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "final_summary");
            Assert.True(File.Exists(Path.Combine(outputDir, "catalog-api-prod-hotfix-release-pack.zip")));
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_61_TemplateWaitResumeReleasePackDemo_Should_Wait_Resume_And_Create_Approval_Bundle()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", "61_template_wait_resume_release_pack_demo.yaml");
        var workflow = new WorkflowTemplateLoader().LoadFromFile(path);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-template-wait-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("request_dir", Path.Combine(tempRoot, "request")),
            ("handoff_dir", Path.Combine(tempRoot, "handoff")),
            ("output_dir", Path.Combine(tempRoot, "output")));

        var outputDir = Path.Combine(tempRoot, "output");

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: "template-wait-release");
            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "template-wait-release",
                new ResumeRequest { SignalType = "approve" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "checkout-api-prod-hotfix-approval-pack.zip")));

            var final = await store.GetRunAsync("template-wait-release");
            Assert.NotNull(final);
            Assert.Equal(StepRunStatus.Completed, final!.Steps["release/approval_pack/wait_for_approval"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_pack/announce_hotfix"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_pack/package_bundle"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["release/approval_pack/final_summary"].Status);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task Example_62_TemplateMultiStagePromotionDemo_Should_Wait_Resume_And_Create_Promotion_Bundle()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", "62_template_multi_stage_promotion_demo.yaml");
        var workflow = new WorkflowTemplateLoader().LoadFromFile(path);
        var tempRoot = Path.Combine(Path.GetTempPath(), $"procedo-demo-template-promotion-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        OverrideWorkspace(
            workflow,
            ("workspace", tempRoot),
            ("request_dir", Path.Combine(tempRoot, "request")),
            ("bundle_dir", Path.Combine(tempRoot, "bundle")),
            ("output_dir", Path.Combine(tempRoot, "output")));

        var outputDir = Path.Combine(tempRoot, "output");

        try
        {
            var engine = new ProcedoWorkflowEngine();
            var registry = CreateRegistry();
            var store = new FileRunStateStore(tempRoot);

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId: "template-promotion");
            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "template-promotion",
                new ResumeRequest { SignalType = "approve" });

            Assert.True(resumed.Success, resumed.Error);
            Assert.True(File.Exists(Path.Combine(outputDir, "identity-api-prod-standard-promotion-pack.zip")));

            var final = await store.GetRunAsync("template-promotion");
            Assert.NotNull(final);
            Assert.Equal(StepRunStatus.Completed, final!.Steps["assess/branch_selection/prod_route"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["rollout/regional_plan/plan_eastus"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["rollout/regional_plan/plan_westus"].Status);
            Assert.Equal(StepRunStatus.Skipped, final.Steps["rollout/regional_plan/plan_centralus"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["approve/handoff/wait_for_promotion_approval"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["approve/handoff/package_bundle"].Status);
            Assert.Equal(StepRunStatus.Completed, final.Steps["approve/handoff/final_summary"].Status);
        }
        finally
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
    }

    private static void OverrideWorkspace(WorkflowDefinition workflow, params (string Key, string Value)[] values)
    {
        foreach (var (key, value) in values)
        {
            workflow.Variables[key] = value;
        }
    }

    private static async Task<WorkflowRunResult> ExecuteAsync(WorkflowDefinition workflow, IExecutionEventSink? sink = null)
    {
        var engine = new ProcedoWorkflowEngine();
        var registry = CreateRegistry();
        return await engine.ExecuteAsync(workflow, registry, new NullLogger(), sink);
    }

    private static IPluginRegistry CreateRegistry()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
        return registry;
    }

    private static WorkflowDefinition LoadWorkflow(string fileName)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var path = Path.Combine(repoRoot, "examples", fileName);
        var yaml = File.ReadAllText(path);
        return new YamlWorkflowParser().Parse(yaml);
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


