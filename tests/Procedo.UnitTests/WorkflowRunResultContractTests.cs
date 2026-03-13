using System.Text.Json;
using Procedo.Core.Models;

namespace Procedo.UnitTests;

public class WorkflowRunResultContractTests
{
    [Fact]
    public void WorkflowRunResult_Should_Serialize_With_Stable_Field_Names()
    {
        var model = new WorkflowRunResult
        {
            Success = false,
            Error = "failed",
            ErrorCode = RuntimeErrorCodes.StepTimeout,
            RunId = "run-123",
            SourcePath = "D:\\repo\\templates\\base.yaml"
        };

        var json = JsonSerializer.Serialize(model);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("Success", out var success));
        Assert.False(success.GetBoolean());

        Assert.True(root.TryGetProperty("Error", out var error));
        Assert.Equal("failed", error.GetString());

        Assert.True(root.TryGetProperty("ErrorCode", out var errorCode));
        Assert.Equal(RuntimeErrorCodes.StepTimeout, errorCode.GetString());

        Assert.True(root.TryGetProperty("RunId", out var runId));
        Assert.Equal("run-123", runId.GetString());

        Assert.True(root.TryGetProperty("SourcePath", out var sourcePath));
        Assert.Equal("D:\\repo\\templates\\base.yaml", sourcePath.GetString());
    }
}
