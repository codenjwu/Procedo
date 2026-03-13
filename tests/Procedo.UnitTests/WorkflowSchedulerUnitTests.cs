using Procedo.Core.Models;
using Procedo.Engine.Graph;
using Procedo.Engine.Scheduling;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class WorkflowSchedulerUnitTests
{
    [Fact]
    public async Task ExecuteJobAsync_Should_Run_Ready_Steps_And_Log_Running_State()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "test.ok" },
                new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" } }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new OkStep());

        var logger = new TestLogger();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, logger, null, CancellationToken.None);

        Assert.True(success);
        Assert.Contains(logger.InfoMessages, m => m.Contains("Running [s1/j1/a]"));
        Assert.Contains(logger.InfoMessages, m => m.Contains("Running [s1/j1/b]"));
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Fail_When_Result_Is_Waiting_But_Not_Success()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps = { new StepDefinition { Step = "a", Type = "test.wait" } }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.wait", () => new WaitingStep());

        var logger = new TestLogger();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, logger, null, CancellationToken.None);

        Assert.False(success);
        Assert.Contains(logger.ErrorMessages, m => m.Contains("waiting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Copy_Outputs_Into_StepScoped_Variables()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "produce", Type = "test.produce" },
                new StepDefinition { Step = "consume", Type = "test.consume", DependsOn = { "produce" } }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.produce", () => new ProduceStep());
        registry.Register("test.consume", () => new ConsumeStep());

        var logger = new TestLogger();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, logger, null, CancellationToken.None);

        Assert.True(success);
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Report_Deadlock_When_No_Node_Can_Progress()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "test.ok", DependsOn = { "b" } },
                new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" } }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new OkStep());

        var logger = new TestLogger();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, logger, null, CancellationToken.None);

        Assert.False(success);
        Assert.Contains(logger.ErrorMessages, m => m.Contains("deadlock", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class OkStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class WaitingStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = false, Waiting = true, Error = "waiting" });
    }

    private sealed class ProduceStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["value"] = 99 }
            });
    }

    private sealed class ConsumeStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            var ok = context.Variables.TryGetValue("produce.value", out var value) && Equals(value, 99);
            return Task.FromResult(new StepResult { Success = ok, Error = ok ? null : "missing variable" });
        }
    }

    private sealed class TestLogger : ILogger
    {
        public List<string> InfoMessages { get; } = new();
        public List<string> ErrorMessages { get; } = new();

        public void LogError(string message) => ErrorMessages.Add(message);
        public void LogInformation(string message) => InfoMessages.Add(message);
        public void LogWarning(string message) { }
    }
}



