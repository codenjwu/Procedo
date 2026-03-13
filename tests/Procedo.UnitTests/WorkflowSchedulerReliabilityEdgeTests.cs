using System.Diagnostics;
using Procedo.Core.Models;
using Procedo.Engine.Graph;
using Procedo.Engine.Scheduling;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class WorkflowSchedulerReliabilityEdgeTests
{
    [Fact]
    public async Task ExecuteJobAsync_Should_Not_Retry_When_Plugin_Is_Missing_Even_If_Retries_Configured()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "a", Type = "missing.plugin", Retries = 5 }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        var logger = new CapturingLogger();

        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, logger);

        Assert.False(success);
        Assert.Contains(logger.ErrorMessages, m => m.Contains("No plugin registered", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.InfoMessages, m => m.Contains("attempt 2/", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExecuteJobAsync_Should_Fail_Quickly_On_Timeout_Even_When_Step_Ignores_CancellationToken()
    {
        var job = new JobDefinition
        {
            Job = "j1",
            Steps =
            {
                new StepDefinition { Step = "slow", Type = "test.uncancelable", TimeoutMs = 30, Retries = 0 }
            }
        };

        var graph = new ExecutionGraphBuilder().Build(job);
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.uncancelable", () => new UncancelableSlowStep());

        var watch = Stopwatch.StartNew();
        var success = await new WorkflowScheduler().ExecuteJobAsync(
            "run1", "wf", "s1", "j1", null, graph, registry, new CapturingLogger());
        watch.Stop();

        Assert.False(success);
        Assert.True(watch.ElapsedMilliseconds < 250, $"Expected bounded timeout failure, elapsed={watch.ElapsedMilliseconds}ms");
    }

    private sealed class UncancelableSlowStep : IProcedoStep
    {
        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            await Task.Delay(500);
            return new StepResult { Success = true };
        }
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> InfoMessages { get; } = new();
        public List<string> ErrorMessages { get; } = new();

        public void LogError(string message) => ErrorMessages.Add(message);
        public void LogInformation(string message) => InfoMessages.Add(message);
        public void LogWarning(string message) { }
    }
}
