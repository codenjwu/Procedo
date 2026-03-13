using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEngineComprehensiveTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Throw_For_Null_Workflow()
    {
        var engine = new ProcedoWorkflowEngine();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => engine.ExecuteAsync(null!, new PluginRegistry(), new NullLogger()));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Succeed_For_Workflow_Without_Stages()
    {
        var workflow = new WorkflowDefinition { Name = "empty" };
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, new PluginRegistry(), new NullLogger());

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Succeed_For_Stage_Without_Jobs()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "no_jobs",
            Stages = { new StageDefinition { Stage = "s1" } }
        };

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, new PluginRegistry(), new NullLogger());

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Succeed_For_Job_Without_Steps()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "no_steps",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs = { new JobDefinition { Job = "j1" } }
                }
            }
        };

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, new PluginRegistry(), new NullLogger());

        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_For_Unknown_Dependency()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "invalid_dep",
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
                                    DependsOn = { "missing" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new SuccessStep());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger()));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Stop_On_First_Failed_Job_And_Not_Run_Next_Stage()
    {
        var events = new List<string>();

        var workflow = new WorkflowDefinition
        {
            Name = "stop_on_fail",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job1",
                            Steps =
                            {
                                new StepDefinition { Step = "fail", Type = "test.fail" }
                            }
                        }
                    }
                },
                new StageDefinition
                {
                    Stage = "stage2",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job2",
                            Steps =
                            {
                                new StepDefinition { Step = "after", Type = "test.after" }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep(events, "fail"));
        registry.Register("test.after", () => new SuccessStep(events, "after"));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.False(result.Success);
        Assert.Equal(["fail"], events);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Run_Multiple_Stages_And_Jobs_In_Sequence_When_Successful()
    {
        var events = new List<string>();

        var workflow = new WorkflowDefinition
        {
            Name = "sequence",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job1",
                            Steps = { new StepDefinition { Step = "a", Type = "test.a" } }
                        },
                        new JobDefinition
                        {
                            Job = "job2",
                            Steps = { new StepDefinition { Step = "b", Type = "test.b" } }
                        }
                    }
                },
                new StageDefinition
                {
                    Stage = "stage2",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job3",
                            Steps = { new StepDefinition { Step = "c", Type = "test.c" } }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.a", () => new SuccessStep(events, "a"));
        registry.Register("test.b", () => new SuccessStep(events, "b"));
        registry.Register("test.c", () => new SuccessStep(events, "c"));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.True(result.Success);
        Assert.Equal(["a", "b", "c"], events);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Honor_PreCanceled_Token()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var workflow = new WorkflowDefinition
        {
            Name = "cancel",
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
                            Steps = { new StepDefinition { Step = "a", Type = "test.ok" } }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new SuccessStep());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), cts.Token));
    }

    private sealed class SuccessStep(List<string>? events = null, string? marker = null) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (events is not null && marker is not null)
            {
                events.Add(marker);
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class FailStep(List<string>? events = null, string? marker = null) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (events is not null && marker is not null)
            {
                events.Add(marker);
            }

            return Task.FromResult(new StepResult { Success = false, Error = "failed" });
        }
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
