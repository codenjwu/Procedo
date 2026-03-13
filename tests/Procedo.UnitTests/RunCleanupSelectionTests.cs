using System.Reflection;
using Procedo.Core.Runtime;

namespace Procedo.UnitTests;

public class RunCleanupSelectionTests
{
    [Fact]
    public void ShouldDeleteRun_Should_Select_Completed_Runs_When_Requested()
    {
        var options = CreateOptions(deleteCompleted: true);
        var run = new WorkflowRunState { Status = RunStatus.Completed, UpdatedAtUtc = DateTimeOffset.UtcNow };

        Assert.True(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Select_Failed_Runs_When_Requested()
    {
        var options = CreateOptions(deleteFailed: true);
        var run = new WorkflowRunState { Status = RunStatus.Failed, UpdatedAtUtc = DateTimeOffset.UtcNow };

        Assert.True(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Select_Old_Runs_When_Threshold_Is_Exceeded()
    {
        var options = CreateOptions(deleteAllOlderThan: TimeSpan.FromHours(1));
        var run = new WorkflowRunState { Status = RunStatus.Completed, UpdatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2) };

        Assert.True(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Keep_Recent_Runs_When_Threshold_Is_Not_Exceeded()
    {
        var options = CreateOptions(deleteAllOlderThan: TimeSpan.FromHours(1));
        var run = new WorkflowRunState { Status = RunStatus.Completed, UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10) };

        Assert.False(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Select_Waiting_Runs_When_Waiting_Threshold_Is_Exceeded()
    {
        var options = CreateOptions(deleteWaitingOlderThan: TimeSpan.FromHours(1));
        var run = new WorkflowRunState
        {
            Status = RunStatus.Waiting,
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            WaitingSinceUtc = DateTimeOffset.UtcNow.AddHours(-2)
        };

        Assert.True(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Keep_Recent_Waiting_Runs_When_Waiting_Threshold_Is_Not_Exceeded()
    {
        var options = CreateOptions(deleteWaitingOlderThan: TimeSpan.FromHours(1));
        var run = new WorkflowRunState
        {
            Status = RunStatus.Waiting,
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            WaitingSinceUtc = DateTimeOffset.UtcNow.AddMinutes(-20)
        };

        Assert.False(InvokeShouldDeleteRun(run, options));
    }

    [Fact]
    public void ShouldDeleteRun_Should_Fall_Back_To_UpdatedUtc_When_WaitingSinceUtc_Is_Missing()
    {
        var options = CreateOptions(deleteWaitingOlderThan: TimeSpan.FromHours(1));
        var run = new WorkflowRunState
        {
            Status = RunStatus.Waiting,
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            WaitingSinceUtc = null
        };

        Assert.True(InvokeShouldDeleteRun(run, options));
    }

    private static bool InvokeShouldDeleteRun(WorkflowRunState run, object options)
    {
        var runtimeOptionsType = typeof(Procedo.Runtime.Program).GetNestedType("RuntimeOptions", BindingFlags.NonPublic);
        Assert.NotNull(runtimeOptionsType);
        var method = typeof(Procedo.Runtime.Program).GetMethod("ShouldDeleteRun", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (bool)method!.Invoke(null, new[] { run, options })!;
    }

    private static object CreateOptions(bool deleteCompleted = false, bool deleteFailed = false, TimeSpan? deleteAllOlderThan = null, TimeSpan? deleteWaitingOlderThan = null)
    {
        var runtimeOptionsType = typeof(Procedo.Runtime.Program).GetNestedType("RuntimeOptions", BindingFlags.NonPublic);
        Assert.NotNull(runtimeOptionsType);
        var options = Activator.CreateInstance(runtimeOptionsType!);
        Assert.NotNull(options);

        runtimeOptionsType!.GetProperty("DeleteCompleted")!.SetValue(options, deleteCompleted);
        runtimeOptionsType.GetProperty("DeleteFailed")!.SetValue(options, deleteFailed);
        runtimeOptionsType.GetProperty("DeleteAllOlderThan")!.SetValue(options, deleteAllOlderThan);
        runtimeOptionsType.GetProperty("DeleteWaitingOlderThan")!.SetValue(options, deleteWaitingOlderThan);
        return options!;
    }
}
