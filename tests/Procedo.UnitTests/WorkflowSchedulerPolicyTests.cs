using System.Diagnostics;
using Procedo.Core.Models;
using Procedo.Engine.Graph;
using Procedo.Engine.Scheduling;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class WorkflowSchedulerPolicyTests
{
    [Fact]
    public async Task ExecuteJobAsync_Should_Retry_And_Succeed_When_Retries_Configured()
    {
        FlakyStep.Attempts = 0;
        var step = new StepDefinition { Step = "flaky", Type = "test.flaky", Retries = 1 };
        var job = new JobDefinition { Job = "j1", Steps = { step } };
        var graph = new ExecutionGraphBuilder().Build(job);

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.flaky", () => new FlakyStep());

        var options = new Procedo.Core.Execution.WorkflowExecutionOptions { RetryInitialBackoffMs = 1, RetryMaxBackoffMs = 2 };
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, new TestLogger(), null, default, options);

        Assert.True(success);
        Assert.Equal(2, FlakyStep.Attempts);
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Fail_On_Timeout_When_Exceeded()
    {
        var step = new StepDefinition { Step = "slow", Type = "test.slow", TimeoutMs = 25 };
        var job = new JobDefinition { Job = "j1", Steps = { step } };
        var graph = new ExecutionGraphBuilder().Build(job);

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.slow", () => new SlowStep(200));

        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, new TestLogger());

        Assert.False(success);
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Run_Independent_Steps_In_Parallel_When_MaxParallelism_Is_Two()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "test.sleep" },
                new StepDefinition { Step = "b", Type = "test.sleep" }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.sleep", () => new SlowStep(150));

        var watch = Stopwatch.StartNew();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, new TestLogger(), null, default, null, 2, false);
        watch.Stop();

        Assert.True(success);
        Assert.True(watch.ElapsedMilliseconds < 280, $"Elapsed {watch.ElapsedMilliseconds}ms should indicate parallel execution.");
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Continue_Independent_Steps_When_ContinueOnError_True()
    {
        var executed = new List<string>();
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "fail", Type = "test.fail" },
                new StepDefinition { Step = "ok", Type = "test.ok" }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep(executed));
        registry.Register("test.ok", () => new OkStep(executed));

        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, new TestLogger(), null, default, null, 2, true);

        Assert.False(success);
        Assert.Contains("ok", executed);
        Assert.Contains("fail", executed);
    }

    private sealed class FlakyStep : IProcedoStep
    {
        public static int Attempts;

        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            Attempts++;
            if (Attempts == 1)
            {
                return Task.FromResult(new StepResult { Success = false, Error = "first failure" });
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class SlowStep(int delayMs) : IProcedoStep
    {
        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            await Task.Delay(delayMs, context.CancellationToken);
            return new StepResult { Success = true };
        }
    }

    private sealed class FailStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add("fail");
            return Task.FromResult(new StepResult { Success = false, Error = "failed" });
        }
    }

    private sealed class OkStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add("ok");
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


