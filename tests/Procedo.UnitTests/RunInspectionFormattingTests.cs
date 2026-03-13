using System.Reflection;
using Procedo.Core.Runtime;

namespace Procedo.UnitTests;

public class RunInspectionFormattingTests
{
    [Fact]
    public void BuildRunInspectionLines_Should_Include_Status_Counts_And_Wait_Details()
    {
        var run = new WorkflowRunState
        {
            RunId = "run-123",
            WorkflowName = "demo",
            WorkflowVersion = 1,
            Status = RunStatus.Waiting,
            CreatedAtUtc = DateTimeOffset.Parse("2026-03-11T20:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-03-11T20:05:00Z"),
            WaitingSinceUtc = DateTimeOffset.Parse("2026-03-11T20:04:00Z"),
            WaitingStepKey = "demo/job/wait_here",
            Steps = new Dictionary<string, StepRunState>(StringComparer.OrdinalIgnoreCase)
            {
                ["demo/job/wait_here"] = new()
                {
                    Stage = "demo",
                    Job = "job",
                    StepId = "wait_here",
                    Status = StepRunStatus.Waiting,
                    Wait = new WaitDescriptor
                    {
                        Type = "signal",
                        Reason = "Waiting for continue"
                    }
                },
                ["demo/job/finished"] = new()
                {
                    Stage = "demo",
                    Job = "job",
                    StepId = "finished",
                    Status = StepRunStatus.Completed,
                    Outputs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["message"] = "done"
                    }
                },
                ["demo/job/skipped"] = new()
                {
                    Stage = "demo",
                    Job = "job",
                    StepId = "skipped",
                    Status = StepRunStatus.Skipped
                }
            }
        };

        var lines = InvokeBuildRunInspectionLines(run);

        Assert.Contains(lines, line => line == "RunId: run-123");
        Assert.Contains(lines, line => line == "Status: Waiting");
        Assert.Contains(lines, line => line == "WaitingStep: wait_here");
        Assert.Contains(lines, line => line == "WaitType: signal");
        Assert.Contains(lines, line => line == "WaitReason: Waiting for continue");
        Assert.Contains(lines, line => line.Contains("StepCounts:", StringComparison.Ordinal) && line.Contains("Skipped=1", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("[Completed] demo/job/finished", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("[Skipped] demo/job/skipped", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("Outputs=message", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildRunInspectionLines_Should_Include_Error_And_Empty_Steps_Message()
    {
        var run = new WorkflowRunState
        {
            RunId = "run-999",
            WorkflowName = "broken",
            WorkflowVersion = 2,
            Status = RunStatus.Failed,
            Error = "boom",
            CreatedAtUtc = DateTimeOffset.Parse("2026-03-11T21:00:00Z"),
            UpdatedAtUtc = DateTimeOffset.Parse("2026-03-11T21:01:00Z")
        };

        var lines = InvokeBuildRunInspectionLines(run);

        Assert.Contains(lines, line => line == "Error: boom");
        Assert.Contains(lines, line => line == "Steps: none");
    }

    private static IReadOnlyList<string> InvokeBuildRunInspectionLines(WorkflowRunState run)
    {
        var method = typeof(Procedo.Runtime.Program).GetMethod("BuildRunInspectionLines", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var value = method!.Invoke(null, new object[] { run });
        var lines = Assert.IsAssignableFrom<IReadOnlyList<string>>(value);
        return lines;
    }
}
