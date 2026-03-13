using System.Text.Json;
using Procedo.Core.Runtime;
using Procedo.Persistence.Stores;

namespace Procedo.UnitTests;

public class FileRunStateStoreTests
{
    [Fact]
    public async Task SaveRunAsync_Then_GetRunAsync_Should_RoundTrip_State()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "run-1",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Running,
                Steps =
                {
                    ["stage/job/step1"] = new StepRunState
                    {
                        Stage = "stage",
                        Job = "job",
                        StepId = "step1",
                        Status = StepRunStatus.Completed,
                        Outputs = new Dictionary<string, object>
                        {
                            ["count"] = 42,
                            ["message"] = "ok"
                        }
                    }
                }
            };

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync("run-1");

            Assert.NotNull(loaded);
            Assert.Equal("wf", loaded!.WorkflowName);
            Assert.Equal(RunStatus.Running, loaded.Status);
            Assert.True(loaded.Steps.TryGetValue("stage/job/step1", out var step));
            Assert.NotNull(step);
            Assert.Equal(StepRunStatus.Completed, step!.Status);
            Assert.Equal(42L, step.Outputs["count"]);
            Assert.Equal("ok", step.Outputs["message"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetRunAsync_Should_Normalize_JsonElement_Nested_Outputs()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "run-2.json");
            var payload = new
            {
                runId = "run-2",
                workflowName = "wf",
                workflowVersion = 1,
                status = RunStatus.Completed,
                error = (string?)null,
                createdAtUtc = DateTimeOffset.UtcNow,
                updatedAtUtc = DateTimeOffset.UtcNow,
                steps = new Dictionary<string, object>
                {
                    ["s/j/step"] = new
                    {
                        stage = "s",
                        job = "j",
                        stepId = "step",
                        status = StepRunStatus.Completed,
                        outputs = new
                        {
                            arr = new object[] { 1, "x", true },
                            obj = new { nested = 12 }
                        }
                    }
                }
            };

            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            var store = new FileRunStateStore(root);
            var loaded = await store.GetRunAsync("run-2");

            Assert.NotNull(loaded);
            var step = loaded!.Steps["s/j/step"];

            var arr = Assert.IsType<List<object>>(step.Outputs["arr"]);
            Assert.Equal(3, arr.Count);
            Assert.Equal(1L, arr[0]);
            Assert.Equal("x", arr[1]);
            Assert.Equal(true, arr[2]);

            var obj = Assert.IsType<Dictionary<string, object>>(step.Outputs["obj"]);
            Assert.Equal(12L, obj["nested"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
