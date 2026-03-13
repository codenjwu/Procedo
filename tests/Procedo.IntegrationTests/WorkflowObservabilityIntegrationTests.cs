using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowObservabilityIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Emit_Run_And_Step_Events_In_Order_For_Success()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "obs_ok",
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
                                new StepDefinition { Step = "a", Type = "test.ok" },
                                new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" } }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new SuccessStep());

        var sink = new InMemorySink();
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink);

        Assert.True(result.Success);
        Assert.Equal(
            [
                ExecutionEventType.RunStarted,
                ExecutionEventType.StepStarted,
                ExecutionEventType.StepCompleted,
                ExecutionEventType.StepStarted,
                ExecutionEventType.StepCompleted,
                ExecutionEventType.RunCompleted
            ],
            sink.Events.Select(e => e.EventType));

        Assert.True(sink.Events.Select(e => e.Sequence).SequenceEqual(Enumerable.Range(1, sink.Events.Count).Select(i => (long)i)));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Emit_Failure_Events_For_Failed_Step()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "obs_fail",
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
                                new StepDefinition { Step = "a", Type = "test.fail" }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep());

        var sink = new InMemorySink();
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink);

        Assert.False(result.Success);
        Assert.Equal(ExecutionEventType.StepFailed, sink.Events[^2].EventType);
        Assert.Equal(ExecutionEventType.RunFailed, sink.Events[^1].EventType);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Skip_Runtime_Condition_False_Step_And_Emit_StepSkipped()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "obs_skip",
            ParameterValues =
            {
                ["environment"] = "dev"
            },
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
                                    Step = "gated",
                                    Type = "test.missing",
                                    Condition = "eq(params.environment, 'prod')"
                                },
                                new StepDefinition
                                {
                                    Step = "after",
                                    Type = "test.ok",
                                    DependsOn = { "gated" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new SuccessStep());

        var sink = new InMemorySink();
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink);

        Assert.True(result.Success);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "gated");
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "after");
        Assert.DoesNotContain(sink.Events, e => e.EventType == ExecutionEventType.StepFailed && e.StepId == "gated");
    }

    private sealed class InMemorySink : IExecutionEventSink
    {
        public List<ExecutionEvent> Events { get; } = new();

        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(executionEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class FailStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = false, Error = "failed" });
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
