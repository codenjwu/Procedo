using Procedo.Core.Models;
using Procedo.Engine.Graph;

namespace Procedo.UnitTests;

public class ExecutionGraphBuilderComprehensiveTests
{
    [Fact]
    public void Build_Should_Return_Empty_Graph_For_Job_Without_Steps()
    {
        var job = new JobDefinition { Job = "empty" };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Empty(graph);
    }

    [Fact]
    public void Build_Should_Create_Node_For_Each_Step()
    {
        var job = new JobDefinition
        {
            Job = "count",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "x" },
                new StepDefinition { Step = "b", Type = "x" },
                new StepDefinition { Step = "c", Type = "x" }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Equal(3, graph.Count);
        Assert.All(graph.Values, n => Assert.Empty(n.Dependencies));
    }

    [Fact]
    public void Build_Should_Link_Multiple_Dependencies_For_Single_Node()
    {
        var job = new JobDefinition
        {
            Job = "fanin",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "x" },
                new StepDefinition { Step = "b", Type = "x" },
                new StepDefinition
                {
                    Step = "merge",
                    Type = "x",
                    DependsOn = { "a", "b" }
                }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Equal(2, graph["merge"].Dependencies.Count);
        Assert.Equal(["a", "b"], graph["merge"].Dependencies.Select(d => d.Step.Step).ToArray());
    }

    [Fact]
    public void Build_Should_Resolve_Dependencies_Case_Insensitively()
    {
        var job = new JobDefinition
        {
            Job = "case",
            Steps =
            {
                new StepDefinition { Step = "Download", Type = "x" },
                new StepDefinition
                {
                    Step = "Parse",
                    Type = "x",
                    DependsOn = { "download" }
                }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Single(graph["Parse"].Dependencies);
        Assert.Equal("Download", graph["Parse"].Dependencies[0].Step.Step);
    }

    [Fact]
    public void Build_Should_Throw_For_Duplicate_Step_Ids()
    {
        var job = new JobDefinition
        {
            Job = "dup",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "x" },
                new StepDefinition { Step = "a", Type = "y" }
            }
        };

        Assert.Throws<ArgumentException>(() => new ExecutionGraphBuilder().Build(job));
    }

    [Fact]
    public void Build_Should_Create_Self_Dependency_When_Configured()
    {
        var job = new JobDefinition
        {
            Job = "self",
            Steps =
            {
                new StepDefinition
                {
                    Step = "a",
                    Type = "x",
                    DependsOn = { "a" }
                }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.Single(graph["a"].Dependencies);
        Assert.Same(graph["a"], graph["a"].Dependencies[0]);
    }
}

