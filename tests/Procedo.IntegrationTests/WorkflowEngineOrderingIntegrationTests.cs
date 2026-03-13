using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowEngineOrderingIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Run_Dependent_Steps_In_Order()
    {
        var executed = new List<string>();

        var workflow = new WorkflowDefinition
        {
            Name = "ordering",
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
                                new StepDefinition { Step = "download", Type = "test.download" },
                                new StepDefinition
                                {
                                    Step = "parse",
                                    Type = "test.parse",
                                    DependsOn = { "download" }
                                },
                                new StepDefinition
                                {
                                    Step = "save",
                                    Type = "test.save",
                                    DependsOn = { "parse" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.download", () => new TrackStep("download", executed));
        registry.Register("test.parse", () => new TrackStep("parse", executed));
        registry.Register("test.save", () => new TrackStep("save", executed));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new TestLogger());

        Assert.True(result.Success);
        Assert.Equal(["download", "parse", "save"], executed);
    }

    private sealed class TrackStep(string name, List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(name);
            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class TestLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
