using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEnginePersistenceStressIntegrationTests
{
    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Complete_Large_Dag_And_Persist_All_Nodes()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildLayeredDagWorkflow(layerCount: 8, width: 10);
            var executionCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.dag", () => new CountingDagStep(executionCount));

            var store = new FileRunStateStore(root);
            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                runId);

            Assert.True(result.Success);

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
            Assert.Equal(80, persisted.Steps.Count);
            Assert.All(persisted.Steps.Values, s => Assert.Equal(StepRunStatus.Completed, s.Status));
            Assert.Equal(80, executionCount.Count);
            Assert.All(executionCount.Values, count => Assert.Equal(1, count));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Converge_After_Multiple_Resume_Cycles_On_Large_Linear_Pipeline()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildLinearWorkflow(length: 60, failStep: "s30");
            var attempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            const int failTimes = 3;
            var round = 0;
            WorkflowRunResult? last = null;

            while (round < 6)
            {
                round++;
                IPluginRegistry registry = new PluginRegistry();
                registry.Register("test.linear", () => new FailNTimesStep(attempts, "s30", failTimes));

                last = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);
                if (last.Success)
                {
                    break;
                }
            }

            Assert.NotNull(last);
            Assert.True(last!.Success);
            Assert.True(round <= 4);

            for (var i = 1; i <= 60; i++)
            {
                var step = $"s{i:D2}";
                Assert.True(attempts.ContainsKey(step));
            }

            for (var i = 1; i <= 29; i++)
            {
                Assert.Equal(1, attempts[$"s{i:D2}"]);
            }

            Assert.Equal(failTimes + 1, attempts["s30"]);

            for (var i = 31; i <= 60; i++)
            {
                Assert.Equal(1, attempts[$"s{i:D2}"]);
            }

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
            Assert.Equal(60, persisted.Steps.Count);
            Assert.All(persisted.Steps.Values, s => Assert.Equal(StepRunStatus.Completed, s.Status));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static WorkflowDefinition BuildLayeredDagWorkflow(int layerCount, int width)
    {
        var job = new JobDefinition { Job = "job" };

        for (var layer = 1; layer <= layerCount; layer++)
        {
            for (var i = 1; i <= width; i++)
            {
                var stepId = $"l{layer:D2}_n{i:D2}";
                var step = new StepDefinition
                {
                    Step = stepId,
                    Type = "test.dag"
                };

                if (layer > 1)
                {
                    for (var parent = 1; parent <= width; parent++)
                    {
                        step.DependsOn.Add($"l{layer - 1:D2}_n{parent:D2}");
                    }
                }

                job.Steps.Add(step);
            }
        }

        return new WorkflowDefinition
        {
            Name = "stress_dag",
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

    private static WorkflowDefinition BuildLinearWorkflow(int length, string failStep)
    {
        var job = new JobDefinition { Job = "job" };

        for (var i = 1; i <= length; i++)
        {
            var stepId = $"s{i:D2}";
            var step = new StepDefinition
            {
                Step = stepId,
                Type = "test.linear"
            };

            if (i > 1)
            {
                step.DependsOn.Add($"s{i - 1:D2}");
            }

            job.Steps.Add(step);
        }

        // keep signature explicit to avoid accidental mismatch during refactor
        _ = failStep;

        return new WorkflowDefinition
        {
            Name = "stress_linear",
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

    private sealed class CountingDagStep(Dictionary<string, int> executionCount) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executionCount.TryGetValue(context.StepId, out var count);
            executionCount[context.StepId] = count + 1;

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["seen"] = executionCount[context.StepId]
                }
            });
        }
    }

    private sealed class FailNTimesStep(Dictionary<string, int> attempts, string stepToFail, int failTimes) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            attempts.TryGetValue(context.StepId, out var count);
            attempts[context.StepId] = count + 1;

            if (string.Equals(context.StepId, stepToFail, StringComparison.OrdinalIgnoreCase)
                && attempts[context.StepId] <= failTimes)
            {
                return Task.FromResult(new StepResult { Success = false, Error = "transient failure" });
            }

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
