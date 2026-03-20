using Procedo.Core.Models;
using Procedo.Core.Abstractions;
using Procedo.Core.Runtime;
using Procedo.Engine;
using Procedo.Engine.Hosting;
using Procedo.DSL;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEngineWaitResumeIntegrationTests
{
    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Pause_Run_When_Step_Returns_Waiting()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(executed));
            registry.Register("test.record", () => new RecordingStep(executed));

            var result = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "wait-run");

            Assert.False(result.Success);
            Assert.True(result.Waiting);
            Assert.Equal(RuntimeErrorCodes.Waiting, result.ErrorCode);
            Assert.Equal("wait_here", result.WaitingStepId);
            Assert.Equal("signal", result.WaitingType);
            Assert.Equal(new[] { "wait_here:wait" }, executed);

            var persisted = await store.GetRunAsync("wait-run");
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Waiting, persisted!.Status);
            Assert.Equal("stage/job/wait_here", persisted.WaitingStepKey);
            Assert.Equal(StepRunStatus.Waiting, persisted.Steps["stage/job/wait_here"].Status);
            Assert.Equal("signal", persisted.Steps["stage/job/wait_here"].Wait?.Type);
            Assert.False(persisted.Steps.ContainsKey("stage/job/after_wait"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_Should_Reexecute_Waiting_Step_And_Continue_Downstream_Steps()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry firstRegistry = new PluginRegistry();
            firstRegistry.Register("test.wait", () => new WaitForSignalStep(executed));
            firstRegistry.Register("test.record", () => new RecordingStep(executed));

            var first = await engine.ExecuteWithPersistenceAsync(workflow, firstRegistry, new NullLogger(), store, "resume-run");
            Assert.True(first.Waiting);

            IPluginRegistry secondRegistry = new PluginRegistry();
            secondRegistry.Register("test.wait", () => new WaitForSignalStep(executed));
            secondRegistry.Register("test.record", () => new RecordingStep(executed));

            var resumed = await engine.ResumeAsync(
                workflow,
                secondRegistry,
                new NullLogger(),
                store,
                "resume-run",
                new ResumeRequest
                {
                    SignalType = "continue",
                    Payload = new Dictionary<string, object>
                    {
                        ["approved_by"] = "operator"
                    }
                });

            Assert.True(resumed.Success);
            Assert.False(resumed.Waiting);
            Assert.Equal(RuntimeErrorCodes.None, resumed.ErrorCode);
            Assert.Equal(new[]
            {
                "wait_here:wait",
                "wait_here:continue",
                "after_wait"
            }, executed);

            var persisted = await store.GetRunAsync("resume-run");
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
            Assert.Null(persisted.WaitingStepKey);
            Assert.Equal(StepRunStatus.Completed, persisted.Steps["stage/job/wait_here"].Status);
            Assert.Equal(StepRunStatus.Completed, persisted.Steps["stage/job/after_wait"].Status);
            Assert.Equal("operator", persisted.Steps["stage/job/wait_here"].Outputs["approved_by"]?.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_Should_Reject_Run_That_Is_Not_Waiting()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = "not-waiting",
                WorkflowName = workflow.Name,
                WorkflowVersion = workflow.Version,
                Status = RunStatus.Completed,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Steps =
                {
                    ["stage/job/wait_here"] = new StepRunState
                    {
                        Stage = "stage",
                        Job = "job",
                        StepId = "wait_here",
                        Status = StepRunStatus.Completed
                    }
                }
            });

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            var result = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                "not-waiting",
                new ResumeRequest { SignalType = "continue" });

            Assert.False(result.Success);
            Assert.False(result.Waiting);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, result.ErrorCode);
            Assert.Contains("not waiting", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Resume_By_Wait_Identity()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(executed));
            registry.Register("test.record", () => new RecordingStep(executed));

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "identity-run");
            Assert.True(first.Waiting);

            var resumed = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "identity-run",
                    SignalType = "continue"
                });

            Assert.True(resumed.Success);
            Assert.Equal(new[]
            {
                "wait_here:wait",
                "wait_here:continue",
                "after_wait"
            }, executed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Fail_When_Multiple_Waits_Match_By_Default()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "match-a")).Waiting);
            await Task.Delay(20);
            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "match-b")).Waiting);

            var result = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    SignalType = "continue"
                });

            Assert.False(result.Success);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, result.ErrorCode);
            Assert.Contains("Multiple waiting runs matched", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Resume_Newest_When_Requested()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(executed));
            registry.Register("test.record", () => new RecordingStep(executed));

            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "older")).Waiting);
            await Task.Delay(20);
            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "newer")).Waiting);

            var resumed = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    SignalType = "continue",
                    MatchBehavior = WaitingRunMatchBehavior.ResumeNewest
                });

            Assert.True(resumed.Success);
            Assert.Equal("newer", resumed.RunId);

            var older = await store.GetRunAsync("older");
            var newer = await store.GetRunAsync("newer");
            Assert.NotNull(older);
            Assert.NotNull(newer);
            Assert.Equal(RunStatus.Waiting, older!.Status);
            Assert.Equal(RunStatus.Completed, newer!.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Fail_When_SignalType_Does_Not_Match_ExpectedSignal()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "signal-mismatch")).Waiting);

            var result = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "signal-mismatch",
                    SignalType = "reject"
                });

            Assert.False(result.Success);
            Assert.Contains("expects signal type", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Fail_Clearly_After_Another_Caller_Already_Resumed_The_Run()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "stale-run")).Waiting);

            var first = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "stale-run",
                    SignalType = "continue"
                });

            var second = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "stale-run",
                    SignalType = "continue"
                });

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, second.ErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Allow_At_Most_One_Winner_Under_Concurrent_Resume()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            Assert.True((await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "concurrent-run")).Waiting);

            var firstTask = engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "concurrent-run",
                    SignalType = "continue"
                });

            var secondTask = engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "concurrent-run",
                    SignalType = "continue"
                });

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Equal(1, results.Count(static result => result.Success));
            Assert.Equal(1, results.Count(static result => !result.Success));
            Assert.Contains(results, static result => result.ErrorCode == RuntimeErrorCodes.InvalidResume);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Support_Multiple_Wait_Resume_Cycles_In_The_Same_Run()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildTwoWaitWorkflow();
            workflow.SourcePath = Path.Combine(root, "two-wait-workflow.yaml");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait.twice", () => new WaitTwiceStep(executed));
            registry.Register("test.record", () => new RecordingStep(executed));

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "two-cycle");
            Assert.True(first.Waiting);

            var second = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "two-cycle:1",
                    SignalType = "continue-1"
                });

            Assert.False(second.Success);
            Assert.True(second.Waiting);

            var third = await engine.ResumeWaitingRunAsync(
                new InMemoryWorkflowResolver(workflow),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "two-cycle:2",
                    SignalType = "continue-2"
                });

            Assert.True(third.Success);
            Assert.Equal(new[]
            {
                "wait_twice:wait-1",
                "wait_twice:continue-1",
                "wait_twice:wait-2",
                "wait_twice:continue-2",
                "after_waits"
            }, executed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Use_Persisted_Workflow_Snapshot_When_Source_File_Changes()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "snapshot-workflow.yaml");
            await File.WriteAllTextAsync(path, BuildWaitWorkflowYaml("after_wait"));

            var workflow = new WorkflowTemplateLoader().LoadFromFile(path);
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);
            var executed = new List<string>();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(executed));
            registry.Register("test.record", () => new RecordingStep(executed));

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "snapshot-run");
            Assert.True(first.Waiting);

            await File.WriteAllTextAsync(path, BuildWaitWorkflowYaml("changed_after_wait"));

            var resumed = await engine.ResumeWaitingRunAsync(
                new FileWorkflowDefinitionResolver(),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "snapshot-run",
                    SignalType = "continue"
                });

            Assert.True(resumed.Success);
            Assert.Contains("after_wait", executed);
            Assert.DoesNotContain("changed_after_wait", executed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Fail_Clearly_When_Persisted_Workflow_Snapshot_Is_Missing()
    {
        var root = CreateTempDirectory();
        try
        {
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = "legacy-snapshot-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                WorkflowSourcePath = Path.Combine(root, "legacy.yaml"),
                Status = RunStatus.Waiting,
                WaitingStepKey = "stage/job/wait",
                WaitingSinceUtc = DateTimeOffset.UtcNow,
                Steps =
                {
                    ["stage/job/wait"] = new StepRunState
                    {
                        Stage = "stage",
                        Job = "job",
                        StepId = "wait",
                        Status = StepRunStatus.Waiting,
                        Wait = new WaitDescriptor
                        {
                            Type = "signal",
                            Key = "legacy-snapshot-run",
                            Metadata = new Dictionary<string, object>
                            {
                                ["expected_signal_type"] = "continue"
                            }
                        }
                    }
                }
            });

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
            registry.Register("test.record", () => new RecordingStep(new List<string>()));

            var result = await engine.ResumeWaitingRunAsync(
                new FileWorkflowDefinitionResolver(),
                registry,
                new NullLogger(),
                store,
                new ResumeWaitingRunRequest
                {
                    WaitType = "signal",
                    WaitKey = "legacy-snapshot-run",
                    SignalType = "continue"
                });

            Assert.False(result.Success);
            Assert.Equal(RuntimeErrorCodes.InvalidResume, result.ErrorCode);
            Assert.Contains("workflow snapshot", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_Should_Require_Conditional_Save_Capability_For_Legacy_Stores()
    {
        var workflow = BuildWaitWorkflow();
        var engine = new ProcedoWorkflowEngine();
        var store = new LegacyRunStateStore();
        var executed = new List<string>();

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.wait", () => new WaitForSignalStep(executed));
        registry.Register("test.record", () => new RecordingStep(executed));

        var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "legacy-store-run");
        Assert.True(first.Waiting);

        var resumed = await engine.ResumeAsync(
            workflow,
            registry,
            new NullLogger(),
            store,
            "legacy-store-run",
            new ResumeRequest { SignalType = "continue" });

        Assert.False(resumed.Success);
        Assert.Equal(RuntimeErrorCodes.ConfigurationInvalid, resumed.ErrorCode);
        Assert.Contains("conditional save", resumed.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new[]
        {
            "wait_here:wait"
        }, executed);
    }

    [Fact]
    public async Task ResumeWaitingRunAsync_Should_Require_Conditional_Save_Capability_For_Legacy_Stores()
    {
        var workflow = BuildWaitWorkflow();
        var engine = new ProcedoWorkflowEngine();
        var store = new LegacyRunStateStore();

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.wait", () => new WaitForSignalStep(new List<string>()));
        registry.Register("test.record", () => new RecordingStep(new List<string>()));

        var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, "legacy-callback");
        Assert.True(first.Waiting);

        var result = await engine.ResumeWaitingRunAsync(
            new InMemoryWorkflowResolver(workflow),
            registry,
            new NullLogger(),
            store,
            new ResumeWaitingRunRequest
            {
                WaitType = "signal",
                WaitKey = "legacy-callback",
                SignalType = "continue"
            });

        Assert.False(result.Success);
        Assert.Equal(RuntimeErrorCodes.ConfigurationInvalid, result.ErrorCode);
        Assert.Contains("conditional save", result.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static WorkflowDefinition BuildWaitWorkflow()
        => new()
        {
            Name = "wait-workflow",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = "wait_here", Type = "test.wait" },
                                new StepDefinition { Step = "after_wait", Type = "test.record", DependsOn = { "wait_here" } }
                            }
                        }
                    }
                }
            }
        };

    private static WorkflowDefinition BuildTwoWaitWorkflow()
        => new()
        {
            Name = "two-wait-workflow",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = "wait_twice", Type = "test.wait.twice" },
                                new StepDefinition { Step = "after_waits", Type = "test.record", DependsOn = { "wait_twice" } }
                            }
                        }
                    }
                }
            }
        };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string BuildWaitWorkflowYaml(string recordStepId)
        => $"""
name: wait-workflow
stages:
  - stage: stage
    jobs:
      - job: job
        steps:
          - step: wait_here
            type: test.wait
          - step: {recordStepId}
            type: test.record
            depends_on:
              - wait_here
""";

    private sealed class WaitForSignalStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (string.Equals(context.Resume?.SignalType, "continue", StringComparison.OrdinalIgnoreCase))
            {
                executed.Add($"{context.StepId}:continue");
                return Task.FromResult(new StepResult
                {
                    Success = true,
                    Outputs = new Dictionary<string, object>
                    {
                        ["approved_by"] = context.Resume.Payload.TryGetValue("approved_by", out var value)
                            ? value ?? string.Empty
                            : string.Empty
                    }
                });
            }

            executed.Add($"{context.StepId}:wait");
            return Task.FromResult(new StepResult
            {
                Waiting = true,
                Wait = new WaitDescriptor
                {
                    Type = "signal",
                    Reason = "Waiting for continue signal",
                    Key = context.RunId
                    ,
                    Metadata = new Dictionary<string, object>
                    {
                        ["expected_signal_type"] = "continue"
                    }
                }
            });
        }
    }

    private sealed class WaitTwiceStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (string.Equals(context.Resume?.SignalType, "continue-2", StringComparison.OrdinalIgnoreCase))
            {
                executed.Add($"{context.StepId}:continue-2");
                return Task.FromResult(new StepResult { Success = true });
            }

            if (string.Equals(context.Resume?.SignalType, "continue-1", StringComparison.OrdinalIgnoreCase))
            {
                executed.Add($"{context.StepId}:continue-1");
                executed.Add($"{context.StepId}:wait-2");
                return Task.FromResult(new StepResult
                {
                    Waiting = true,
                    Wait = new WaitDescriptor
                    {
                        Type = "signal",
                        Reason = "Waiting for second continue signal",
                        Key = $"{context.RunId}:2",
                        Metadata = new Dictionary<string, object>
                        {
                            ["expected_signal_type"] = "continue-2"
                        }
                    },
                    Outputs = new Dictionary<string, object>
                    {
                        ["cycle"] = 1
                    }
                });
            }

            executed.Add($"{context.StepId}:wait-1");
            return Task.FromResult(new StepResult
            {
                Waiting = true,
                Wait = new WaitDescriptor
                {
                    Type = "signal",
                    Reason = "Waiting for first continue signal",
                    Key = $"{context.RunId}:1",
                    Metadata = new Dictionary<string, object>
                    {
                        ["expected_signal_type"] = "continue-1"
                    }
                }
            });
        }
    }

    private sealed class RecordingStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(context.StepId);
            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }

    private sealed class InMemoryWorkflowResolver(WorkflowDefinition workflow) : Procedo.Core.Abstractions.IWorkflowDefinitionResolver
    {
        public Task<WorkflowDefinition> ResolveAsync(PersistedWorkflowReference reference, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(workflow);
        }
    }

    private sealed class LegacyRunStateStore : IRunStateStore
    {
        private readonly Dictionary<string, WorkflowRunState> _runs = new(StringComparer.OrdinalIgnoreCase);

        public Task<WorkflowRunState?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _runs.TryGetValue(runId, out var run);
            return Task.FromResult(run is null ? null : Clone(run));
        }

        public Task<IReadOnlyList<WorkflowRunState>> ListRunsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<WorkflowRunState> runs = _runs.Values.Select(Clone).ToArray();
            return Task.FromResult(runs);
        }

        public Task SaveRunAsync(WorkflowRunState runState, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _runs[runState.RunId] = Clone(runState);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteRunAsync(string runId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_runs.Remove(runId));
        }

        private static WorkflowRunState Clone(WorkflowRunState state)
            => new()
            {
                PersistenceSchemaVersion = state.PersistenceSchemaVersion,
                ConcurrencyVersion = state.ConcurrencyVersion,
                RunId = state.RunId,
                WorkflowName = state.WorkflowName,
                WorkflowVersion = state.WorkflowVersion,
                WorkflowSourcePath = state.WorkflowSourcePath,
                WorkflowDefinitionSnapshot = state.WorkflowDefinitionSnapshot,
                WorkflowDefinitionFingerprint = state.WorkflowDefinitionFingerprint,
                WorkflowParameters = new Dictionary<string, object>(state.WorkflowParameters, StringComparer.OrdinalIgnoreCase),
                Status = state.Status,
                Error = state.Error,
                CreatedAtUtc = state.CreatedAtUtc,
                UpdatedAtUtc = state.UpdatedAtUtc,
                WaitingStepKey = state.WaitingStepKey,
                WaitingSinceUtc = state.WaitingSinceUtc,
                Steps = state.Steps.ToDictionary(
                    static pair => pair.Key,
                    static pair => CloneStep(pair.Value),
                    StringComparer.OrdinalIgnoreCase)
            };

        private static StepRunState CloneStep(StepRunState step)
            => new()
            {
                Stage = step.Stage,
                Job = step.Job,
                StepId = step.StepId,
                Status = step.Status,
                Error = step.Error,
                StartedAtUtc = step.StartedAtUtc,
                CompletedAtUtc = step.CompletedAtUtc,
                Outputs = new Dictionary<string, object>(step.Outputs, StringComparer.OrdinalIgnoreCase),
                Wait = step.Wait is null
                    ? null
                    : new WaitDescriptor
                    {
                        Type = step.Wait.Type,
                        Reason = step.Wait.Reason,
                        Key = step.Wait.Key,
                        Metadata = new Dictionary<string, object>(step.Wait.Metadata, StringComparer.OrdinalIgnoreCase)
                    }
            };
    }
}
