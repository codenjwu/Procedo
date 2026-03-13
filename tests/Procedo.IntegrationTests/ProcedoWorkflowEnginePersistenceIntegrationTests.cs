using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEnginePersistenceIntegrationTests
{
    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Resume_And_Skip_Completed_Steps()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildLinearWorkflow("a", "b", "c");
            var attempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var store = new FileRunStateStore(root);
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry firstRegistry = new PluginRegistry();
            firstRegistry.Register("test.maybe_fail", () => new FailOnceStep(attempts, "b"));
            firstRegistry.Register("test.ok", () => new CountingStep(attempts));

            var firstResult = await engine.ExecuteWithPersistenceAsync(
                workflow,
                firstRegistry,
                new NullLogger(),
                store,
                runId);

            Assert.False(firstResult.Success);
            Assert.Equal(runId, firstResult.RunId);
            Assert.Equal(1, attempts["a"]);
            Assert.Equal(1, attempts["b"]);
            Assert.False(attempts.ContainsKey("c"));

            IPluginRegistry secondRegistry = new PluginRegistry();
            secondRegistry.Register("test.maybe_fail", () => new CountingStep(attempts));
            secondRegistry.Register("test.ok", () => new CountingStep(attempts));

            var secondResult = await engine.ExecuteWithPersistenceAsync(
                workflow,
                secondRegistry,
                new NullLogger(),
                store,
                runId);

            Assert.True(secondResult.Success);
            Assert.Equal(runId, secondResult.RunId);

            Assert.Equal(1, attempts["a"]);
            Assert.Equal(2, attempts["b"]);
            Assert.Equal(1, attempts["c"]);

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
            Assert.All(persisted.Steps.Values, s => Assert.Equal(StepRunStatus.Completed, s.Status));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Record_Failed_Run_Status()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildLinearWorkflow("x");
            var runId = Guid.NewGuid().ToString("N");
            var store = new FileRunStateStore(root);
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new AlwaysFailStep());

            var result = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.False(result.Success);
            Assert.Equal(runId, result.RunId);

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Failed, persisted!.Status);
            Assert.Contains("failed", persisted.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Record_Skipped_Status_For_Runtime_Condition_False_Step()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = new WorkflowDefinition
            {
                Name = "persisted-skip",
                ParameterValues =
                {
                    ["environment"] = "dev"
                },
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
                                    new StepDefinition
                                    {
                                        Step = "gated",
                                        Type = "test.missing",
                                        Condition = "eq(params.environment, 'prod')"
                                    },
                                    new StepDefinition
                                    {
                                        Step = "after",
                                        Type = "test.ok",
                                        DependsOn = { "gated" }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var runId = Guid.NewGuid().ToString("N");
            var store = new FileRunStateStore(root);
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new CountingStep(new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)));

            var result = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.True(result.Success);

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(StepRunStatus.Skipped, persisted!.Steps["stage/job/gated"].Status);
            Assert.Equal(StepRunStatus.Completed, persisted.Steps["stage/job/after"].Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static WorkflowDefinition BuildLinearWorkflow(params string[] steps)
    {
        var job = new JobDefinition { Job = "job" };

        for (var i = 0; i < steps.Length; i++)
        {
            var stepId = steps[i];
            var step = new StepDefinition
            {
                Step = stepId,
                Type = string.Equals(stepId, "b", StringComparison.OrdinalIgnoreCase) ? "test.maybe_fail" : "test.ok"
            };

            if (i > 0)
            {
                step.DependsOn.Add(steps[i - 1]);
            }

            job.Steps.Add(step);
        }

        return new WorkflowDefinition
        {
            Name = "persisted",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage",
                    Jobs = { job }
                }
            }
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingStep(Dictionary<string, int> attempts) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            attempts.TryGetValue(context.StepId, out var count);
            attempts[context.StepId] = count + 1;

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["attempt"] = attempts[context.StepId]
                }
            });
        }
    }

    private sealed class FailOnceStep(Dictionary<string, int> attempts, string targetStep) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            attempts.TryGetValue(context.StepId, out var count);
            attempts[context.StepId] = count + 1;

            if (string.Equals(context.StepId, targetStep, StringComparison.OrdinalIgnoreCase) && attempts[context.StepId] == 1)
            {
                return Task.FromResult(new StepResult { Success = false, Error = "step failed once" });
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class AlwaysFailStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = false, Error = "always failed" });
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
