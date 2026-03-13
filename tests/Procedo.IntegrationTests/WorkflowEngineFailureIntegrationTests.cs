using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowEngineFailureIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Plugin_Is_Missing()
    {
        var workflow = BuildSingleStepWorkflow("missing.step");
        IPluginRegistry registry = new PluginRegistry();

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new TestLogger());

        Assert.False(result.Success);
        Assert.Equal(RuntimeErrorCodes.PluginNotFound, result.ErrorCode);
        Assert.Contains("No plugin registered", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Step_Returns_Failure()
    {
        var workflow = BuildSingleStepWorkflow("test.fail");
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep());

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new TestLogger());

        Assert.False(result.Success);
        Assert.Equal(RuntimeErrorCodes.StepResultFailed, result.ErrorCode);
        Assert.Equal("failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_When_Step_Throws_Exception()
    {
        var workflow = BuildSingleStepWorkflow("test.throw");
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.throw", () => new ThrowStep());

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new TestLogger());

        Assert.False(result.Success);
        Assert.Equal(RuntimeErrorCodes.StepException, result.ErrorCode);
        Assert.Equal("boom", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Fail_On_Cyclic_Dependencies()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "cycle",
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
                                new StepDefinition
                                {
                                    Step = "a",
                                    Type = "test.ok",
                                    DependsOn = { "b" }
                                },
                                new StepDefinition
                                {
                                    Step = "b",
                                    Type = "test.ok",
                                    DependsOn = { "a" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new OkStep());

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new TestLogger());

        Assert.False(result.Success);
        Assert.Equal(RuntimeErrorCodes.SchedulerDeadlock, result.ErrorCode);
    }

    private static WorkflowDefinition BuildSingleStepWorkflow(string type) =>
        new()
        {
            Name = "single",
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
                            Steps = { new StepDefinition { Step = "a", Type = type } }
                        }
                    }
                }
            }
        };

    private sealed class OkStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context) =>
            Task.FromResult(new StepResult { Success = true });
    }

    private sealed class FailStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context) =>
            Task.FromResult(new StepResult { Success = false, Error = "failed" });
    }

    private sealed class ThrowStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class TestLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
