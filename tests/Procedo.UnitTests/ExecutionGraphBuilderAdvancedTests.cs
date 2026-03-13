using Procedo.Core.Models;
using Procedo.Engine.Graph;

namespace Procedo.UnitTests;

public class ExecutionGraphBuilderAdvancedTests
{
    [Fact]
    public void Build_Should_Create_Correct_Adjacency_For_Large_Dag()
    {
        var job = new JobDefinition
        {
            Job = "large_dag",
            Steps =
            {
                new StepDefinition { Step = "extract_users", Type = "x" },
                new StepDefinition { Step = "extract_orders", Type = "x" },
                new StepDefinition { Step = "extract_inventory", Type = "x" },
                new StepDefinition
                {
                    Step = "normalize_users",
                    Type = "x",
                    DependsOn = { "extract_users" }
                },
                new StepDefinition
                {
                    Step = "normalize_orders",
                    Type = "x",
                    DependsOn = { "extract_orders" }
                },
                new StepDefinition
                {
                    Step = "join_sales",
                    Type = "x",
                    DependsOn = { "normalize_users", "normalize_orders" }
                },
                new StepDefinition
                {
                    Step = "score_risk",
                    Type = "x",
                    DependsOn = { "join_sales", "extract_inventory" }
                },
                new StepDefinition
                {
                    Step = "publish",
                    Type = "x",
                    DependsOn = { "score_risk" }
                }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Equal(8, graph.Count);
        Assert.Equal(["extract_users"], Deps(graph, "normalize_users"));
        Assert.Equal(["extract_orders"], Deps(graph, "normalize_orders"));
        Assert.Equal(["normalize_users", "normalize_orders"], Deps(graph, "join_sales"));
        Assert.Equal(["join_sales", "extract_inventory"], Deps(graph, "score_risk"));
        Assert.Equal(["score_risk"], Deps(graph, "publish"));
    }

    [Fact]
    public void Build_Should_Share_Same_Parent_Node_Instance_For_FanOut()
    {
        var job = new JobDefinition
        {
            Job = "fanout",
            Steps =
            {
                new StepDefinition { Step = "root", Type = "x" },
                new StepDefinition { Step = "child_a", Type = "x", DependsOn = { "root" } },
                new StepDefinition { Step = "child_b", Type = "x", DependsOn = { "root" } },
                new StepDefinition { Step = "child_c", Type = "x", DependsOn = { "root" } }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        var root = graph["root"];
        Assert.Same(root, graph["child_a"].Dependencies.Single());
        Assert.Same(root, graph["child_b"].Dependencies.Single());
        Assert.Same(root, graph["child_c"].Dependencies.Single());
    }

    private static string[] Deps(IReadOnlyDictionary<string, ExecutionNode> graph, string stepId)
        => graph[stepId].Dependencies.Select(x => x.Step.Step).ToArray();
}
