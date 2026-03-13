using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowReliabilityExtendedIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Stay_Stable_Over_Mini_Soak_Runs()
    {
        var workflow = BuildSimpleWorkflow();

        for (var i = 0; i < 120; i++)
        {
            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new OkStep());

            var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());
            Assert.True(result.Success);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Resume_Correctly_After_Failure_And_Complete_Remaining_Steps()
    {
        var root = Path.Combine(Path.GetTempPath(), $"procedo-restart-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "restart_recovery",
                Stages =
                {
                    new StageDefinition
                    {
                        Stage = "s1",
                        Jobs =
                        {
                            new JobDefinition
                            {
                                Job = "j1",
                                Steps =
                                {
                                    new StepDefinition { Step = "s1", Type = "test.ok" },
                                    new StepDefinition { Step = "s2", Type = "test.flaky", DependsOn = { "s1" } },
                                    new StepDefinition { Step = "s3", Type = "test.ok", DependsOn = { "s2" } }
                                }
                            }
                        }
                    }
                }
            };

            var store = new FileRunStateStore(root);

            IPluginRegistry firstRegistry = new PluginRegistry();
            firstRegistry.Register("test.ok", () => new OkStep());
            firstRegistry.Register("test.flaky", () => new AlwaysFailStep());

            var engine = new ProcedoWorkflowEngine();
            var first = await engine.ExecuteWithPersistenceAsync(workflow, firstRegistry, new NullLogger(), store, runId);
            Assert.False(first.Success);

            var afterFirst = await store.GetRunAsync(runId);
            Assert.NotNull(afterFirst);
            Assert.True(afterFirst!.Steps.TryGetValue("s1/j1/s1", out var s1));
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, s1!.Status);
            Assert.True(afterFirst.Steps.TryGetValue("s1/j1/s2", out var s2));
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Failed, s2!.Status);

            IPluginRegistry secondRegistry = new PluginRegistry();
            secondRegistry.Register("test.ok", () => new OkStep());
            secondRegistry.Register("test.flaky", () => new OkStep());

            var resumed = await engine.ExecuteWithPersistenceAsync(workflow, secondRegistry, new NullLogger(), store, runId);
            Assert.True(resumed.Success);

            var afterResume = await store.GetRunAsync(runId);
            Assert.NotNull(afterResume);
            Assert.Equal(Procedo.Core.Runtime.RunStatus.Completed, afterResume!.Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, afterResume.Steps["s1/j1/s1"].Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, afterResume.Steps["s1/j1/s2"].Status);
            Assert.Equal(Procedo.Core.Runtime.StepRunStatus.Completed, afterResume.Steps["s1/j1/s3"].Status);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    private static WorkflowDefinition BuildSimpleWorkflow() => new()
    {
        Name = "mini_soak",
        Stages =
        {
            new StageDefinition
            {
                Stage = "s1",
                Jobs =
                {
                    new JobDefinition
                    {
                        Job = "j1",
                        Steps =
                        {
                            new StepDefinition { Step = "a", Type = "test.ok" },
                            new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" } },
                            new StepDefinition { Step = "c", Type = "test.ok", DependsOn = { "b" } }
                        }
                    }
                }
            }
        }
    };

    private sealed class OkStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class AlwaysFailStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = false, Error = "simulated crash" });
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
