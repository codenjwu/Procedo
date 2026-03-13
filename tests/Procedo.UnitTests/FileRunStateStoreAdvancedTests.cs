using System.Text.Json;
using Procedo.Core.Runtime;
using Procedo.Persistence.Stores;

namespace Procedo.UnitTests;

public class FileRunStateStoreAdvancedTests
{
    [Fact]
    public async Task GetRunAsync_Should_Return_Null_When_File_Does_Not_Exist()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = await store.GetRunAsync("missing-run");
            Assert.Null(run);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Throw_When_RunId_Is_Empty()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState { RunId = "" };

            await Assert.ThrowsAsync<ArgumentException>(() => store.SaveRunAsync(run));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("../unsafe")]
    [InlineData("bad/name")]
    [InlineData("bad\\name")]
    public async Task GetRunAsync_Should_Throw_For_Invalid_RunId(string runId)
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            await Assert.ThrowsAsync<ArgumentException>(() => store.GetRunAsync(runId));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Update_UpdatedAtUtc_And_Keep_CreatedAtUtc()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var created = DateTimeOffset.UtcNow.AddHours(-1);
            var run = new WorkflowRunState
            {
                RunId = "run-time-check",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                CreatedAtUtc = created,
                UpdatedAtUtc = DateTimeOffset.MinValue
            };

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync(run.RunId);

            Assert.NotNull(loaded);
            Assert.Equal(created, loaded!.CreatedAtUtc);
            Assert.True(loaded.UpdatedAtUtc > created);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Set_CreatedAtUtc_When_Not_Provided()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "created-at-default",
                WorkflowName = "wf",
                WorkflowVersion = 1
            };

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync(run.RunId);

            Assert.NotNull(loaded);
            Assert.NotEqual(default, loaded!.CreatedAtUtc);
            Assert.True(loaded.UpdatedAtUtc >= loaded.CreatedAtUtc);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetRunAsync_Should_Handle_Case_Insensitive_Step_And_Output_Keys()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "case-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Completed,
                Steps =
                {
                    ["Stage/Job/StepA"] = new StepRunState
                    {
                        Stage = "Stage",
                        Job = "Job",
                        StepId = "StepA",
                        Status = StepRunStatus.Completed,
                        Outputs = new Dictionary<string, object>
                        {
                            ["Result"] = "OK"
                        }
                    }
                }
            };

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync("case-run");

            Assert.NotNull(loaded);
            Assert.True(loaded!.Steps.TryGetValue("stage/job/stepa", out var step));
            Assert.NotNull(step);
            Assert.Equal("OK", step!.Outputs["result"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Overwrite_Previous_Run_File()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "overwrite-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Running
            };

            await store.SaveRunAsync(run);

            run.Status = RunStatus.Completed;
            run.Error = null;
            await store.SaveRunAsync(run);

            var loaded = await store.GetRunAsync("overwrite-run");
            Assert.NotNull(loaded);
            Assert.Equal(RunStatus.Completed, loaded!.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Write_Current_Persistence_Schema_Version_And_Remove_Temp_Files()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "schema-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Completed
            };

            await store.SaveRunAsync(run);

            var path = Path.Combine(root, "schema-run.json");
            var json = await File.ReadAllTextAsync(path);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(FileRunStateStore.CurrentPersistenceSchemaVersion, document.RootElement.GetProperty("PersistenceSchemaVersion").GetInt32());
            Assert.Empty(Directory.GetFiles(root, "*.tmp"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetRunAsync_Should_Default_Legacy_Run_State_To_Current_Schema_Version()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "legacy.json");
            var payload = new
            {
                runId = "legacy",
                workflowName = "wf",
                workflowVersion = 1,
                status = RunStatus.Completed,
                createdAtUtc = DateTimeOffset.UtcNow,
                updatedAtUtc = DateTimeOffset.UtcNow,
                steps = new Dictionary<string, object>()
            };

            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            var store = new FileRunStateStore(root);
            var loaded = await store.GetRunAsync("legacy");

            Assert.NotNull(loaded);
            Assert.Equal(FileRunStateStore.CurrentPersistenceSchemaVersion, loaded!.PersistenceSchemaVersion);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetRunAsync_Should_Throw_InvalidDataException_For_Unsupported_Future_Schema()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "future.json");
            var payload = new
            {
                persistenceSchemaVersion = FileRunStateStore.CurrentPersistenceSchemaVersion + 1,
                runId = "future",
                workflowName = "wf",
                workflowVersion = 1,
                status = RunStatus.Completed,
                createdAtUtc = DateTimeOffset.UtcNow,
                updatedAtUtc = DateTimeOffset.UtcNow,
                steps = new Dictionary<string, object>()
            };

            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            var store = new FileRunStateStore(root);
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => store.GetRunAsync("future"));
            Assert.Contains("schema version", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ListRunsAsync_Should_Return_Persisted_Runs_With_Waiting_Runs_First()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = "completed-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Completed,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10)
            });

            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = "waiting-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Waiting,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
                WaitingSinceUtc = DateTimeOffset.UtcNow,
                WaitingStepKey = "stage/job/wait"
            });

            var runs = await store.ListRunsAsync();

            Assert.Equal(2, runs.Count);
            Assert.Equal("waiting-run", runs[0].RunId);
            Assert.Contains(runs, run => run.RunId == "completed-run");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task GetRunAsync_Should_Throw_InvalidDataException_For_Malformed_Json()
    {
        var root = CreateTempDirectory();
        try
        {
            var path = Path.Combine(root, "broken.json");
            await File.WriteAllTextAsync(path, "{ not valid json");

            var store = new FileRunStateStore(root);
            var ex = await Assert.ThrowsAsync<InvalidDataException>(() => store.GetRunAsync("broken"));
            Assert.Contains("malformed JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(path, ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task SaveRunAsync_Should_Roundtrip_Waiting_Metadata()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var run = new WorkflowRunState
            {
                RunId = "waiting-run",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Waiting,
                WaitingStepKey = "stage/job/wait",
                WaitingSinceUtc = DateTimeOffset.UtcNow,
                Steps =
                {
                    ["stage/job/wait"] = new StepRunState
                    {
                        Stage = "stage",
                        Job = "job",
                        StepId = "wait",
                        Status = StepRunStatus.Waiting,
                        Wait = new WaitDescriptor
                        {
                            Type = "signal",
                            Reason = "Waiting for continue",
                            Key = "run-123",
                            Metadata = new Dictionary<string, object>
                            {
                                ["attempt"] = 1,
                                ["source"] = "test"
                            }
                        }
                    }
                }
            };

            await store.SaveRunAsync(run);
            var loaded = await store.GetRunAsync(run.RunId);

            Assert.NotNull(loaded);
            Assert.Equal(RunStatus.Waiting, loaded!.Status);
            Assert.Equal("stage/job/wait", loaded.WaitingStepKey);
            var step = loaded.Steps["stage/job/wait"];
            Assert.Equal(StepRunStatus.Waiting, step.Status);
            Assert.NotNull(step.Wait);
            Assert.Equal("signal", step.Wait!.Type);
            Assert.Equal("Waiting for continue", step.Wait.Reason);
            Assert.Equal("run-123", step.Wait.Key);
            Assert.Equal(1L, Convert.ToInt64(step.Wait.Metadata["attempt"]));
            Assert.Equal("test", step.Wait.Metadata["source"]?.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task DeleteRunAsync_Should_Remove_Existing_Run_File()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = "delete-me",
                WorkflowName = "wf",
                WorkflowVersion = 1,
                Status = RunStatus.Completed
            });

            var deleted = await store.DeleteRunAsync("delete-me");
            var loaded = await store.GetRunAsync("delete-me");

            Assert.True(deleted);
            Assert.Null(loaded);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteRunAsync_Should_Return_False_When_Run_Does_Not_Exist()
    {
        var root = CreateTempDirectory();
        try
        {
            var store = new FileRunStateStore(root);
            var deleted = await store.DeleteRunAsync("missing-run");

            Assert.False(deleted);
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



