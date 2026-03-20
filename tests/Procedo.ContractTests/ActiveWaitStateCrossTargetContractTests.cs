using Procedo.Core.Runtime;

namespace Procedo.ContractTests;

public sealed class ActiveWaitStateCrossTargetContractTests
{
    [Fact]
    public void ActiveWaitState_Defaults_Should_Be_Usable()
    {
        var wait = new ActiveWaitState();

        Assert.Equal(string.Empty, wait.RunId);
        Assert.Equal(string.Empty, wait.WorkflowName);
        Assert.Equal(string.Empty, wait.Stage);
        Assert.Equal(string.Empty, wait.Job);
        Assert.Equal(string.Empty, wait.StepId);
        Assert.Equal(string.Empty, wait.StepPath);
        Assert.Equal(string.Empty, wait.WaitType);
        Assert.NotNull(wait.Metadata);
    }

    [Fact]
    public void WaitingRunQuery_Defaults_Should_Be_Usable()
    {
        var query = new WaitingRunQuery();

        Assert.True(query.IncludeMetadata);
        Assert.Null(query.WorkflowName);
        Assert.Null(query.WaitType);
        Assert.Null(query.WaitKey);
    }

    [Fact]
    public void ResumeWaitingRunRequest_Defaults_Should_Be_Usable()
    {
        var request = new ResumeWaitingRunRequest();

        Assert.Equal(string.Empty, request.WaitType);
        Assert.Equal(string.Empty, request.SignalType);
        Assert.Equal(WaitingRunMatchBehavior.FailWhenMultiple, request.MatchBehavior);
        Assert.NotNull(request.Payload);
    }
}
