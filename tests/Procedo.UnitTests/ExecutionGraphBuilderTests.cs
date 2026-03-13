using Procedo.Core.Models;
using Procedo.Engine.Graph;

namespace Procedo.UnitTests;

public class ExecutionGraphBuilderTests
{
    [Fact]
    public void Build_Should_Link_Dependency_Nodes()
    {
        var job = new JobDefinition
        {
            Job = "pipeline",
            Steps =
            {
                new StepDefinition { Step = "download", Type = "system.echo" },
                new StepDefinition
                {
                    Step = "parse",
                    Type = "system.echo",
                    DependsOn = { "download" }
                }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);

        Assert.True(graph.ContainsKey("download"));
        Assert.True(graph.ContainsKey("parse"));
        Assert.Single(graph["parse"].Dependencies);
        Assert.Equal("download", graph["parse"].Dependencies[0].Step.Step);
    }

    [Fact]
    public void Build_Should_Throw_For_Unknown_Dependency()
    {
        var job = new JobDefinition
        {
            Job = "pipeline",
            Steps =
            {
                new StepDefinition
                {
                    Step = "parse",
                    Type = "system.echo",
                    DependsOn = { "missing" }
                }
            }
        };

        var builder = new ExecutionGraphBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.Build(job));
    }
}
