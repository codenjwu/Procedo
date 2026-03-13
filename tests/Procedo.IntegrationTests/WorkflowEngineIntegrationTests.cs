using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowEngineIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Run_Dependency_Chain_And_Propagate_Outputs()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "integration",
            Version = 1,
            Stages =
            {
                new StageDefinition
                {
                    Stage = "main",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = "produce", Type = "test.produce" },
                                new StepDefinition
                                {
                                    Step = "consume",
                                    Type = "test.consume",
                                    DependsOn = { "produce" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.produce", () => new ProduceStep());
        registry.Register("test.consume", () => new ConsumeStep());

        var engine = new ProcedoWorkflowEngine();
        var logger = new ConsoleLogger();

        var result = await engine.ExecuteAsync(workflow, registry, logger);

        Assert.True(result.Success);
    }

    private sealed class ProduceStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            var outputs = new Dictionary<string, object> { ["value"] = 42 };
            return Task.FromResult(new StepResult { Success = true, Outputs = outputs });
        }
    }

    private sealed class ConsumeStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            var ok = context.Variables.TryGetValue("produce.value", out var value) && Equals(value, 42);
            return Task.FromResult(new StepResult
            {
                Success = ok,
                Error = ok ? null : "Expected produce.value = 42"
            });
        }
    }
}
