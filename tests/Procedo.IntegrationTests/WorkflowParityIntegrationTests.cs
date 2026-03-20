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

public sealed class WorkflowParityIntegrationTests
{
    [Fact]
    public async Task Example_66_RetryParityDemo_Should_Match_Between_Persisted_And_NonPersisted()
    {
        var nonPersistedRoot = CreateTempDirectory("procedo-parity-retry-np");
        var persistedRoot = CreateTempDirectory("procedo-parity-retry-p");
        var nonPersistedOutput = Path.Combine(nonPersistedRoot, "retry-parity.json");
        var persistedOutput = Path.Combine(persistedRoot, "retry-parity.json");

        try
        {
            var nonPersistedWorkflow = LoadWorkflow("66_retry_parity_demo.yaml");
            OverrideWorkspace(nonPersistedWorkflow, nonPersistedRoot, nonPersistedOutput);
            var nonPersistedSink = new InMemorySink();
            var nonPersisted = await ExecuteAsync(nonPersistedWorkflow, nonPersistedSink);

            var persistedWorkflow = LoadWorkflow("66_retry_parity_demo.yaml");
            OverrideWorkspace(persistedWorkflow, persistedRoot, persistedOutput);
            var persistedSink = new InMemorySink();
            var storeRoot = Path.Combine(persistedRoot, "state");
            Directory.CreateDirectory(storeRoot);
            var persisted = await ExecuteWithPersistenceAsync(persistedWorkflow, "retry-parity", storeRoot, persistedSink);

            Assert.True(nonPersisted.Success, nonPersisted.Error);
            Assert.True(persisted.Success, persisted.Error);
            Assert.Equal(File.ReadAllText(nonPersistedOutput), File.ReadAllText(persistedOutput));
            using var nonPersistedJson = JsonDocument.Parse(File.ReadAllText(nonPersistedOutput));
            using var persistedJson = JsonDocument.Parse(File.ReadAllText(persistedOutput));
            Assert.Equal(2, nonPersistedJson.RootElement.GetProperty("result").GetProperty("attempt").GetInt32());
            Assert.Equal(2, persistedJson.RootElement.GetProperty("result").GetProperty("attempt").GetInt32());
            Assert.Equal(1, CountEvents(nonPersistedSink, ExecutionEventType.StepCompleted, "flaky_call"));
            Assert.Equal(1, CountEvents(persistedSink, ExecutionEventType.StepCompleted, "flaky_call"));

            var store = new FileRunStateStore(storeRoot);
            var runState = await store.GetRunAsync("retry-parity");
            Assert.NotNull(runState);
            Assert.Equal(RunStatus.Completed, runState!.Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["resilience/retry/flaky_call"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["resilience/retry/write_snapshot"].Status);
        }
        finally
        {
            TryDelete(nonPersistedRoot);
            TryDelete(persistedRoot);
        }
    }

    [Fact]
    public async Task Example_67_TimeoutParityDemo_Should_Fail_With_Same_ErrorCode_In_Both_Modes()
    {
        var persistedRoot = CreateTempDirectory("procedo-parity-timeout");

        try
        {
            var nonPersistedSink = new InMemorySink();
            var nonPersisted = await ExecuteAsync(LoadWorkflow("67_timeout_parity_demo.yaml"), nonPersistedSink);

            var persistedSink = new InMemorySink();
            var storeRoot = Path.Combine(persistedRoot, "state");
            Directory.CreateDirectory(storeRoot);
            var persisted = await ExecuteWithPersistenceAsync(LoadWorkflow("67_timeout_parity_demo.yaml"), "timeout-parity", storeRoot, persistedSink);

            Assert.False(nonPersisted.Success);
            Assert.False(persisted.Success);
            Assert.Equal(RuntimeErrorCodes.StepTimeout, nonPersisted.ErrorCode);
            Assert.Equal(RuntimeErrorCodes.StepTimeout, persisted.ErrorCode);
            Assert.Equal(1, CountEvents(nonPersistedSink, ExecutionEventType.StepFailed, "slow_step"));
            Assert.Equal(1, CountEvents(persistedSink, ExecutionEventType.StepFailed, "slow_step"));

            var store = new FileRunStateStore(storeRoot);
            var runState = await store.GetRunAsync("timeout-parity");
            Assert.NotNull(runState);
            Assert.Equal(RunStatus.Failed, runState!.Status);
            Assert.Equal(StepRunStatus.Failed, runState.Steps["timeout/policy/slow_step"].Status);
        }
        finally
        {
            TryDelete(persistedRoot);
        }
    }

    [Fact]
    public async Task Example_68_ContinueOnErrorParityDemo_Should_Run_Sibling_Work_And_Fail_Run_In_Both_Modes()
    {
        var nonPersistedRoot = CreateTempDirectory("procedo-parity-coe-np");
        var persistedRoot = CreateTempDirectory("procedo-parity-coe-p");
        var nonPersistedOutput = Path.Combine(nonPersistedRoot, "continue-on-error-parity.json");
        var persistedOutput = Path.Combine(persistedRoot, "continue-on-error-parity.json");

        try
        {
            var nonPersistedWorkflow = LoadWorkflow("68_continue_on_error_parity_demo.yaml");
            OverrideWorkspace(nonPersistedWorkflow, nonPersistedRoot, nonPersistedOutput);
            var nonPersistedSink = new InMemorySink();
            var nonPersisted = await ExecuteAsync(nonPersistedWorkflow, nonPersistedSink);

            var persistedWorkflow = LoadWorkflow("68_continue_on_error_parity_demo.yaml");
            OverrideWorkspace(persistedWorkflow, persistedRoot, persistedOutput);
            var persistedSink = new InMemorySink();
            var storeRoot = Path.Combine(persistedRoot, "state");
            Directory.CreateDirectory(storeRoot);
            var persisted = await ExecuteWithPersistenceAsync(persistedWorkflow, "coe-parity", storeRoot, persistedSink);

            Assert.False(nonPersisted.Success);
            Assert.False(persisted.Success);
            Assert.True(File.Exists(nonPersistedOutput));
            Assert.True(File.Exists(persistedOutput));
            Assert.Equal(File.ReadAllText(nonPersistedOutput), File.ReadAllText(persistedOutput));
            Assert.Equal(1, CountEvents(nonPersistedSink, ExecutionEventType.StepFailed, "bad"));
            Assert.Equal(1, CountEvents(persistedSink, ExecutionEventType.StepFailed, "bad"));
            Assert.Equal(1, CountEvents(nonPersistedSink, ExecutionEventType.StepCompleted, "write_snapshot"));
            Assert.Equal(1, CountEvents(persistedSink, ExecutionEventType.StepCompleted, "write_snapshot"));

            var store = new FileRunStateStore(storeRoot);
            var runState = await store.GetRunAsync("coe-parity");
            Assert.NotNull(runState);
            Assert.Equal(RunStatus.Failed, runState!.Status);
            Assert.Equal(StepRunStatus.Failed, runState.Steps["parity/branch/bad"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["parity/branch/good_json"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["parity/branch/write_snapshot"].Status);
        }
        finally
        {
            TryDelete(nonPersistedRoot);
            TryDelete(persistedRoot);
        }
    }

    [Fact]
    public async Task Example_69_MaxParallelismParityDemo_Should_Start_Only_Two_Sleep_Steps_Before_First_Completion_In_Both_Modes()
    {
        var persistedRoot = CreateTempDirectory("procedo-parity-parallel");

        try
        {
            var nonPersistedSink = new InMemorySink();
            var nonPersisted = await ExecuteAsync(LoadWorkflow("69_max_parallelism_parity_demo.yaml"), nonPersistedSink);

            var persistedSink = new InMemorySink();
            var storeRoot = Path.Combine(persistedRoot, "state");
            Directory.CreateDirectory(storeRoot);
            var persisted = await ExecuteWithPersistenceAsync(LoadWorkflow("69_max_parallelism_parity_demo.yaml"), "parallel-parity", storeRoot, persistedSink);

            Assert.True(nonPersisted.Success, nonPersisted.Error);
            Assert.True(persisted.Success, persisted.Error);
            Assert.Equal(2, CountSleepStartsBeforeFirstSleepCompletion(nonPersistedSink));
            Assert.Equal(2, CountSleepStartsBeforeFirstSleepCompletion(persistedSink));
            Assert.Equal(4, CountEvents(nonPersistedSink, ExecutionEventType.StepCompleted, prefix: "sleep_"));
            Assert.Equal(4, CountEvents(persistedSink, ExecutionEventType.StepCompleted, prefix: "sleep_"));
        }
        finally
        {
            TryDelete(persistedRoot);
        }
    }

    [Fact]
    public async Task Example_70_WaitResumeParityDemo_Should_Wait_Then_Resume_And_Persist_Final_State()
    {
        var workflow = LoadWorkflow("70_wait_resume_parity_demo.yaml");
        var persistedRoot = CreateTempDirectory("procedo-parity-wait");
        var outputFile = Path.Combine(persistedRoot, "wait-resume-parity.json");
        OverrideWorkspace(workflow, persistedRoot, outputFile);
        var storeRoot = Path.Combine(persistedRoot, "state");
        Directory.CreateDirectory(storeRoot);

        try
        {
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(storeRoot);

            var first = await engine.ExecuteWithPersistenceAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "wait-resume-parity",
                sink);

            Assert.False(first.Success);
            Assert.True(first.Waiting);
            Assert.Equal("signal", first.WaitingType);

            var resumed = await engine.ResumeAsync(
                workflow,
                CreateRegistry(),
                new NullLogger(),
                store,
                "wait-resume-parity",
                new ResumeRequest
                {
                    SignalType = "continue",
                    Payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["ticket"] = "CHG-700"
                    }
                },
                sink);

            Assert.True(resumed.Success, resumed.Error);
            using var json = JsonDocument.Parse(File.ReadAllText(outputFile));
            var result = json.RootElement.GetProperty("result");
            Assert.Equal("continue", result.GetProperty("signal_type").GetString());
            Assert.Equal("CHG-700", result.GetProperty("payload").GetProperty("ticket").GetString());

            var runState = await store.GetRunAsync("wait-resume-parity");
            Assert.NotNull(runState);
            Assert.Equal(RunStatus.Completed, runState!.Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["parity/resume/wait_for_signal"].Status);
            Assert.Equal(StepRunStatus.Completed, runState.Steps["parity/resume/write_snapshot"].Status);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunWaiting);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunResumed);
        }
        finally
        {
            TryDelete(persistedRoot);
        }
    }

    private static WorkflowDefinition LoadWorkflow(string fileName)
    {
        var path = Path.Combine(ExampleCatalogInventory.GetRepoRoot(), "examples", fileName);
        var yaml = File.ReadAllText(path);
        return new YamlWorkflowParser().Parse(yaml);
    }

    private static void OverrideWorkspace(WorkflowDefinition workflow, string workspaceRoot, string outputFile)
    {
        workflow.Variables["workspace"] = workspaceRoot;
        workflow.Variables["output_file"] = outputFile;
    }

    private static async Task<WorkflowRunResult> ExecuteAsync(WorkflowDefinition workflow, InMemorySink sink)
        => await new ProcedoWorkflowEngine().ExecuteAsync(workflow, CreateRegistry(), new NullLogger(), sink);

    private static async Task<WorkflowRunResult> ExecuteWithPersistenceAsync(WorkflowDefinition workflow, string runId, string stateDir, InMemorySink sink)
        => await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
            workflow,
            CreateRegistry(),
            new NullLogger(),
            new FileRunStateStore(stateDir),
            runId,
            sink);

    private static IPluginRegistry CreateRegistry()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.AddSystemPlugin();
        registry.AddDemoPlugin();
        return registry;
    }

    private static int CountEvents(InMemorySink sink, ExecutionEventType type, string? stepId = null, string? prefix = null)
        => sink.Events.Count(e =>
            e.EventType == type
            && (stepId is null || string.Equals(e.StepId, stepId, StringComparison.OrdinalIgnoreCase))
            && (prefix is null || (e.StepId?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ?? false)));

    private static int CountSleepStartsBeforeFirstSleepCompletion(InMemorySink sink)
    {
        var ordered = sink.Events.OrderBy(static e => e.Sequence).ToList();
        var firstCompletionIndex = ordered.FindIndex(e =>
            e.EventType == ExecutionEventType.StepCompleted
            && (e.StepId?.StartsWith("sleep_", StringComparison.OrdinalIgnoreCase) ?? false));

        Assert.True(firstCompletionIndex >= 0, "Expected at least one completed sleep step.");

        return ordered
            .Take(firstCompletionIndex)
            .Count(e => e.EventType == ExecutionEventType.StepStarted
                && (e.StepId?.StartsWith("sleep_", StringComparison.OrdinalIgnoreCase) ?? false));
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
