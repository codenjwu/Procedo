using Procedo.Observability;

namespace Procedo.UnitTests;

public class ExecutionEventRequiredFieldGuardTests
{
    [Fact]
    public void TryValidateRequiredFields_Should_Fail_When_RunId_Missing()
    {
        var evt = new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = "",
            WorkflowName = "wf"
        };

        var ok = ExecutionEventGuards.TryValidateRequiredFields(evt, out var error);

        Assert.False(ok);
        Assert.Contains("RunId", error);
    }

    [Fact]
    public void TryValidateRequiredFields_Should_Fail_When_WorkflowName_Missing()
    {
        var evt = new ExecutionEvent
        {
            EventType = ExecutionEventType.RunStarted,
            RunId = "run-1",
            WorkflowName = ""
        };

        var ok = ExecutionEventGuards.TryValidateRequiredFields(evt, out var error);

        Assert.False(ok);
        Assert.Contains("WorkflowName", error);
    }

    [Fact]
    public void TryValidateRequiredFields_Should_Fail_When_Step_Event_Missing_Scope()
    {
        var evt = new ExecutionEvent
        {
            EventType = ExecutionEventType.StepStarted,
            RunId = "run-1",
            WorkflowName = "wf",
            Stage = "s1",
            Job = "j1"
        };

        var ok = ExecutionEventGuards.TryValidateRequiredFields(evt, out var error);

        Assert.False(ok);
        Assert.Contains("Step scope", error);
    }

    [Fact]
    public void TryValidateRequiredFields_Should_Require_Scope_For_StepWaiting_Event()
    {
        var evt = new ExecutionEvent
        {
            EventType = ExecutionEventType.StepWaiting,
            RunId = "run-1",
            WorkflowName = "wf"
        };

        var ok = ExecutionEventGuards.TryValidateRequiredFields(evt, out var error);

        Assert.False(ok);
        Assert.Contains("Step scope", error);
    }
    [Fact]
    public void TryValidateRequiredFields_Should_Pass_For_Valid_Step_Event()
    {
        var evt = new ExecutionEvent
        {
            EventType = ExecutionEventType.StepCompleted,
            RunId = "run-1",
            WorkflowName = "wf",
            Stage = "s1",
            Job = "j1",
            StepId = "a",
            StepType = "system.echo"
        };

        var ok = ExecutionEventGuards.TryValidateRequiredFields(evt, out var error);

        Assert.True(ok);
        Assert.Null(error);
    }
}

