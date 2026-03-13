using Procedo.Core.Models;
using Procedo.Engine.Graph;

namespace Procedo.UnitTests;

public class ExecutionGraphPropertyTests
{
    [Fact]
    public void Build_Should_Preserve_Dag_Dependency_Invariants_Across_Random_Cases()
    {
        var random = new Random(20260309);

        for (var caseIndex = 0; caseIndex < 75; caseIndex++)
        {
            var job = BuildRandomDagJob(random, minNodes: 4, maxNodes: 18);
            var graph = new ExecutionGraphBuilder().Build(job);

            Assert.Equal(job.Steps.Count, graph.Count);

            foreach (var step in job.Steps)
            {
                Assert.True(graph.ContainsKey(step.Step));

                var node = graph[step.Step];
                var expectedDeps = step.DependsOn;
                var actualDeps = node.Dependencies.Select(d => d.Step.Step).ToList();

                Assert.Equal(expectedDeps.Count, actualDeps.Count);
                Assert.Equal(expectedDeps, actualDeps);
            }

            foreach (var step in job.Steps)
            {
                var childIndex = ParseIndex(step.Step);
                foreach (var dep in step.DependsOn)
                {
                    var parentIndex = ParseIndex(dep);
                    Assert.True(parentIndex < childIndex,
                        $"Detected non-DAG edge {dep} -> {step.Step} in case {caseIndex}.");
                }
            }
        }
    }

    private static JobDefinition BuildRandomDagJob(Random random, int minNodes, int maxNodes)
    {
        var nodeCount = random.Next(minNodes, maxNodes + 1);
        var steps = new List<StepDefinition>(nodeCount);

        for (var i = 0; i < nodeCount; i++)
        {
            var step = new StepDefinition
            {
                Step = $"n{i}",
                Type = "test.step"
            };

            if (i > 0)
            {
                var maxParents = Math.Min(3, i);
                var parentCount = random.Next(0, maxParents + 1);
                var candidates = Enumerable.Range(0, i).OrderBy(_ => random.Next()).Take(parentCount);
                foreach (var p in candidates)
                {
                    step.DependsOn.Add($"n{p}");
                }
            }

            steps.Add(step);
        }

        return new JobDefinition
        {
            Job = "random",
            Steps = steps
        };
    }

    private static int ParseIndex(string stepId) => int.Parse(stepId[1..]);
}
