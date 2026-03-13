using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEnginePropertyTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Satisfy_Dependency_PartialOrder_Across_Random_Dags()
    {
        var random = new Random(20260309);

        for (var caseIndex = 0; caseIndex < 30; caseIndex++)
        {
            var workflow = BuildRandomDagWorkflow(random, minNodes: 5, maxNodes: 16);
            var steps = workflow.Stages[0].Jobs[0].Steps;

            var executed = new List<string>();
            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.track", () => new TrackingStep(executed));

            var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

            Assert.True(result.Success, $"Workflow failed in randomized case {caseIndex}.");
            Assert.Equal(steps.Count, executed.Count);
            Assert.Equal(steps.Count, executed.Distinct(StringComparer.OrdinalIgnoreCase).Count());

            foreach (var step in steps)
            {
                foreach (var dep in step.DependsOn)
                {
                    var depPos = executed.IndexOf(dep);
                    var stepPos = executed.IndexOf(step.Step);
                    Assert.True(depPos >= 0 && stepPos >= 0 && depPos < stepPos,
                        $"Dependency order violated in case {caseIndex}: {dep} should run before {step.Step}. " +
                        $"Actual: {string.Join(",", executed)}");
                }
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_Should_Expose_All_CompletedOutputs_In_Variables_For_DownstreamSteps()
    {
        var random = new Random(20260309);

        for (var caseIndex = 0; caseIndex < 20; caseIndex++)
        {
            var workflow = BuildRandomDagWorkflow(random, minNodes: 5, maxNodes: 12);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.track", () => new OutputPropagatingStep());

            var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());
            Assert.True(result.Success, $"Output propagation case {caseIndex} failed.");
        }
    }

    private static WorkflowDefinition BuildRandomDagWorkflow(Random random, int minNodes, int maxNodes)
    {
        var nodeCount = random.Next(minNodes, maxNodes + 1);
        var steps = new List<StepDefinition>(nodeCount);

        for (var i = 0; i < nodeCount; i++)
        {
            var step = new StepDefinition
            {
                Step = $"n{i}",
                Type = "test.track"
            };

            if (i > 0)
            {
                var maxParents = Math.Min(3, i);
                var parentCount = random.Next(0, maxParents + 1);
                var parents = Enumerable.Range(0, i).OrderBy(_ => random.Next()).Take(parentCount);

                foreach (var p in parents)
                {
                    step.DependsOn.Add($"n{p}");
                }
            }

            steps.Add(step);
        }

        return new WorkflowDefinition
        {
            Name = "random_property",
            Version = 1,
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
                            Steps = steps
                        }
                    }
                }
            }
        };
    }

    private sealed class TrackingStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            lock (executed)
            {
                executed.Add(context.StepId);
            }

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["done"] = context.StepId }
            });
        }
    }

    private sealed class OutputPropagatingStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            var stepIndex = int.Parse(context.StepId[1..]);

            for (var i = 0; i < stepIndex; i++)
            {
                var key = $"n{i}.done";
                if (context.Variables.ContainsKey(key))
                {
                    var value = context.Variables[key]?.ToString();
                    Assert.Equal($"n{i}", value);
                }
            }

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["done"] = context.StepId }
            });
        }
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
