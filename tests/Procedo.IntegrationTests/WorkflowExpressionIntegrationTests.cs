using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowExpressionIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Resolve_Step_Output_Expressions_In_Inputs()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "expr",
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
                            Steps =
                            {
                                new StepDefinition { Step = "a", Type = "test.produce" },
                                new StepDefinition
                                {
                                    Step = "b",
                                    Type = "test.consume",
                                    DependsOn = { "a" },
                                    With =
                                    {
                                        ["message"] = "value=${steps.a.outputs.value}"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.produce", () => new ProduceStep());
        registry.Register("test.consume", () => new ConsumeStep("value=hello"));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.True(result.Success);
    }

    private sealed class ProduceStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = "hello"
                }
            });
    }

    private sealed class ConsumeStep(string expected) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (!context.Inputs.TryGetValue("message", out var value) || !Equals(value, expected))
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = $"Expression not resolved. Expected '{expected}', got '{value}'."
                });
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
