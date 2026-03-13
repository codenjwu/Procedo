using System.Collections.Concurrent;
using System.Diagnostics;
using Procedo.Plugin.Demo;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class DemoPluginStepTests
{
    [Fact]
    public async Task FlakyStep_Should_Fail_Then_Succeed_Based_On_FailTimes()
    {
        var counts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var step = new FlakyStep(counts);
        var context = CreateContext("run-a", "flaky", new Dictionary<string, object>
        {
            ["message"] = "transient",
            ["fail_times"] = 2
        });

        var first = await step.ExecuteAsync(context);
        var second = await step.ExecuteAsync(context);
        var third = await step.ExecuteAsync(context);

        Assert.False(first.Success);
        Assert.False(second.Success);
        Assert.True(third.Success);
        Assert.Equal("transient", third.Outputs["message"]);
        Assert.Equal(3, third.Outputs["attempt"]);
    }

    [Fact]
    public async Task FlakyStep_Should_Default_To_One_Failure_When_FailTimes_Missing()
    {
        var counts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var step = new FlakyStep(counts);
        var context = CreateContext("run-b", "flaky-default", new Dictionary<string, object>
        {
            ["message"] = "default"
        });

        var first = await step.ExecuteAsync(context);
        var second = await step.ExecuteAsync(context);

        Assert.False(first.Success);
        Assert.True(second.Success);
        Assert.Equal(2, second.Outputs["attempt"]);
    }

    [Fact]
    public async Task FailOnceStep_Should_Fail_First_Then_Succeed_For_Same_Run_And_Step()
    {
        var counts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var step = new FailOnceStep(counts);
        var context = CreateContext("run-c", "fail-once", new Dictionary<string, object> { ["message"] = "boom" });

        var first = await step.ExecuteAsync(context);
        var second = await step.ExecuteAsync(context);

        Assert.False(first.Success);
        Assert.Equal("boom", first.Error);
        Assert.True(second.Success);
        Assert.Equal("boom", second.Outputs["message"]);
        Assert.Equal(2, second.Outputs["attempt"]);
    }

    [Fact]
    public async Task FailOnceStep_Should_Track_Run_And_Step_Independently()
    {
        var counts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var step = new FailOnceStep(counts);

        var runA = CreateContext("run-a", "shared-step", new Dictionary<string, object>());
        var runB = CreateContext("run-b", "shared-step", new Dictionary<string, object>());

        var a1 = await step.ExecuteAsync(runA);
        var a2 = await step.ExecuteAsync(runA);
        var b1 = await step.ExecuteAsync(runB);

        Assert.False(a1.Success);
        Assert.True(a2.Success);
        Assert.False(b1.Success);
    }

    [Fact]
    public async Task SleepStep_Should_Honor_CancellationToken()
    {
        var step = new SleepStep();
        using var cts = new CancellationTokenSource(25);
        var context = CreateContext("run-sleep", "sleep", new Dictionary<string, object> { ["milliseconds"] = 500 }, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => step.ExecuteAsync(context));
    }

    [Fact]
    public async Task SleepStep_Should_Treat_Negative_Milliseconds_As_Zero()
    {
        var step = new SleepStep();
        var sw = Stopwatch.StartNew();

        var result = await step.ExecuteAsync(CreateContext("run-sleep", "sleep-neg", new Dictionary<string, object> { ["milliseconds"] = -1 }));

        sw.Stop();
        Assert.True(result.Success);
        Assert.True(sw.ElapsedMilliseconds < 200);
    }

    [Fact]
    public async Task FailStep_Should_Return_Input_Message()
    {
        var step = new FailStep();

        var result = await step.ExecuteAsync(CreateContext("run-fail", "fail", new Dictionary<string, object> { ["message"] = "intentional" }));

        Assert.False(result.Success);
        Assert.Equal("intentional", result.Error);
    }

    [Fact]
    public async Task CancelStep_Should_Return_Default_Cancellation_Message()
    {
        var step = new CancelStep();

        var result = await step.ExecuteAsync(CreateContext("run-cancel", "cancel", new Dictionary<string, object>()));

        Assert.False(result.Success);
        Assert.Equal("Cancellation requested by demo.cancel.", result.Error);
    }

    [Fact]
    public async Task QualityStep_Should_Pass_And_Emit_Outputs_When_Above_Threshold()
    {
        var step = new QualityStep();

        var result = await step.ExecuteAsync(CreateContext("run-q", "quality", new Dictionary<string, object>
        {
            ["message"] = "dataset-a",
            ["score"] = 91,
            ["threshold"] = 80
        }));

        Assert.True(result.Success);
        Assert.Equal("dataset-a", result.Outputs["subject"]);
        Assert.Equal(91, result.Outputs["score"]);
        Assert.Equal(80, result.Outputs["threshold"]);
        Assert.Equal(true, result.Outputs["passed"]);
    }

    [Fact]
    public async Task QualityStep_Should_Fail_When_Below_Threshold()
    {
        var step = new QualityStep();

        var result = await step.ExecuteAsync(CreateContext("run-q", "quality", new Dictionary<string, object>
        {
            ["message"] = "dataset-b",
            ["score"] = 70,
            ["threshold"] = 80
        }));

        Assert.False(result.Success);
        Assert.Contains("dataset-b", result.Error);
        Assert.Equal(false, result.Outputs["passed"]);
    }

    [Fact]
    public async Task ScoreStep_Should_Be_Deterministic_For_Same_Input_Message()
    {
        var step = new ScoreStep();
        var contextA = CreateContext("run-score-1", "score", new Dictionary<string, object> { ["message"] = "signal-a" });
        var contextB = CreateContext("run-score-2", "score", new Dictionary<string, object> { ["message"] = "signal-a" });

        var first = await step.ExecuteAsync(contextA);
        var second = await step.ExecuteAsync(contextB);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.Outputs["score"], second.Outputs["score"]);
        Assert.Equal(first.Outputs["band"], second.Outputs["band"]);

        var score = Assert.IsType<int>(first.Outputs["score"]);
        Assert.InRange(score, 60, 99);
    }

    [Fact]
    public void AddDemoPlugin_Should_Register_All_Demo_Step_Types()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.AddDemoPlugin();

        Assert.True(registry.TryResolve("demo.flaky", out _));
        Assert.True(registry.TryResolve("demo.sleep", out _));
        Assert.True(registry.TryResolve("demo.fail", out _));
        Assert.True(registry.TryResolve("demo.fail_once", out _));
        Assert.True(registry.TryResolve("demo.cancel", out _));
        Assert.True(registry.TryResolve("demo.quality", out _));
        Assert.True(registry.TryResolve("demo.score", out _));
    }

    private static StepContext CreateContext(
        string runId,
        string stepId,
        IDictionary<string, object> inputs,
        CancellationToken cancellationToken = default)
        => new()
        {
            RunId = runId,
            StepId = stepId,
            Inputs = inputs,
            Variables = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
            Logger = new NullLogger(),
            CancellationToken = cancellationToken
        };

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
