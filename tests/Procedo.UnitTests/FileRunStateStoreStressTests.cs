using Procedo.Core.Runtime;
using Procedo.Persistence.Stores;

namespace Procedo.UnitTests;

public class FileRunStateStoreStressTests
{
    [Fact]
    public async Task SaveAndLoad_Should_Handle_Large_RunState_With_Many_Steps_And_Outputs()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "stress-run",
                WorkflowName = "stress",
                WorkflowVersion = 1,
                Status = RunStatus.Running,
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            const int stepCount = 500;
            for (var i = 0; i < stepCount; i++)
            {
                var stepId = $"step-{i:D4}";
                run.Steps[$"stage/job/{stepId}"] = new StepRunState
                {
                    Stage = "stage",
                    Job = "job",
                    StepId = stepId,
                    Status = StepRunStatus.Completed,
                    Outputs = new Dictionary<string, object>
                    {
                        ["index"] = i,
                        ["label"] = $"value-{i}",
                        ["flags"] = new object[] { i % 2 == 0, i % 3 == 0 },
                        ["meta"] = new Dictionary<string, object>
                        {
                            ["bucket"] = i / 10,
                            ["token"] = $"t-{i}"
                        }
                    }
                };
            }

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync("stress-run");

            Assert.NotNull(loaded);
            Assert.Equal(stepCount, loaded!.Steps.Count);

            var sample = loaded.Steps["stage/job/step-0420"];
            Assert.Equal(StepRunStatus.Completed, sample.Status);
            Assert.Equal(420L, sample.Outputs["index"]);
            Assert.Equal("value-420", sample.Outputs["label"]);

            var flags = Assert.IsType<List<object>>(sample.Outputs["flags"]);
            Assert.Equal(true, flags[0]);
            Assert.Equal(true, flags[1]);

            var meta = Assert.IsType<Dictionary<string, object>>(sample.Outputs["meta"]);
            Assert.Equal(42L, meta["bucket"]);
            Assert.Equal("t-420", meta["token"]);
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
