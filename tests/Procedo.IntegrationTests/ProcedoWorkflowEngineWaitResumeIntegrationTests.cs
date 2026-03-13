using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine;
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

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
}
