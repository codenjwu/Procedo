using Procedo.Core.Models;
using Procedo.Core.Runtime;
using Procedo.Engine;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEnginePersistenceAdvancedIntegrationTests
{
    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Generate_RunId_And_Persist_Run_File_When_Not_Provided()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = BuildSingleStepWorkflow("only", "test.ok");
            var engine = new ProcedoWorkflowEngine();
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new SuccessStep());

            var result = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store);

            Assert.True(result.Success);
            Assert.False(string.IsNullOrWhiteSpace(result.RunId));

            var persisted = await store.GetRunAsync(result.RunId!);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Not_Reexecute_When_All_Steps_Already_Completed()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildLinearWorkflow();
            var executed = new List<string>();
            var store = new FileRunStateStore(root);

            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = runId,
                WorkflowName = workflow.Name,
                WorkflowVersion = workflow.Version,
                Status = RunStatus.Completed,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Steps =
                {
                    ["stage/job/a"] = new StepRunState { Stage = "stage", Job = "job", StepId = "a", Status = StepRunStatus.Completed },
                    ["stage/job/b"] = new StepRunState { Stage = "stage", Job = "job", StepId = "b", Status = StepRunStatus.Completed },
                    ["stage/job/c"] = new StepRunState { Stage = "stage", Job = "job", StepId = "c", Status = StepRunStatus.Completed }
                }
            });

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new RecordingStep(executed));
            registry.Register("test.check", () => new RecordingStep(executed));

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.True(result.Success);
            Assert.Empty(executed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Rehydrate_Variables_From_Previously_Completed_Dependency()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "rehydrate",
                Stages =
                {
                    new StageDefinition
                    {
                        Stage = "stage",
                        Jobs =
                        {
                            new JobDefinition
                            {
                                Job = "job",
                                Steps =
                                {
                                    new StepDefinition { Step = "a", Type = "test.unused" },
                                    new StepDefinition { Step = "b", Type = "test.requires", DependsOn = { "a" } }
                                }
                            }
                        }
                    }
                }
            };

            var store = new FileRunStateStore(root);
            await store.SaveRunAsync(new WorkflowRunState
            {
                RunId = runId,
                WorkflowName = workflow.Name,
                WorkflowVersion = workflow.Version,
                Status = RunStatus.Failed,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                Steps =
                {
                    ["stage/job/a"] = new StepRunState
                    {
                        Stage = "stage",
                        Job = "job",
                        StepId = "a",
                        Status = StepRunStatus.Completed,
                        Outputs = new Dictionary<string, object>
                        {
                            ["token"] = "abc123"
                        }
                    }
                }
            });

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.unused", () => new ThrowIfExecutedStep("a should be skipped"));
            registry.Register("test.requires", () => new RequireVariableStep("a.token", "abc123"));

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.True(result.Success);
            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
            Assert.Equal(StepRunStatus.Completed, persisted.Steps["stage/job/b"].Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Resume_Across_Stages_And_Only_Run_Remaining_Steps()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var executed = new List<string>();
            var attempts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var workflow = BuildMultiStageWorkflow();
            var store = new FileRunStateStore(root);
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry firstRegistry = new PluginRegistry();
            firstRegistry.Register("test.ok", () => new RecordingStep(executed));
            firstRegistry.Register("test.fail.once", () => new FailOnceByStepStep(attempts, "s2b", executed));

            var firstResult = await engine.ExecuteWithPersistenceAsync(workflow, firstRegistry, new NullLogger(), store, runId);
            Assert.False(firstResult.Success);

            IPluginRegistry secondRegistry = new PluginRegistry();
            secondRegistry.Register("test.ok", () => new RecordingStep(executed));
            secondRegistry.Register("test.fail.once", () => new FailOnceByStepStep(attempts, "s2b", executed));

            var secondResult = await engine.ExecuteWithPersistenceAsync(workflow, secondRegistry, new NullLogger(), store, runId);
            Assert.True(secondResult.Success);

            Assert.Equal(1, executed.Count(x => x == "s1a"));
            Assert.Equal(1, executed.Count(x => x == "s1b"));
            Assert.Equal(2, attempts["s2b"]);
            Assert.Equal(2, executed.Count(x => x == "s2b"));
            Assert.Equal(1, executed.Count(x => x == "s2c"));

            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Completed, persisted!.Status);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Persist_Step_Failure_For_Missing_Plugin()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildSingleStepWorkflow("missing", "test.missing");
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.False(result.Success);
            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Failed, persisted!.Status);
            var step = persisted.Steps["stage/job/missing"];
            Assert.Equal(StepRunStatus.Failed, step.Status);
            Assert.Contains("No plugin registered", step.Error ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Persist_Step_Failure_When_Exception_Is_Thrown()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildSingleStepWorkflow("explode", "test.throw");
            var store = new FileRunStateStore(root);

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.throw", () => new ThrowingStep("boom"));

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId);

            Assert.False(result.Success);
            var persisted = await store.GetRunAsync(runId);
            Assert.NotNull(persisted);
            Assert.Equal(RunStatus.Failed, persisted!.Status);
            var step = persisted.Steps["stage/job/explode"];
            Assert.Equal(StepRunStatus.Failed, step.Status);
            Assert.Equal("boom", step.Error);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Surface_Corrupted_Run_State_With_Clear_Error()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = BuildSingleStepWorkflow("only", "test.ok");
            var store = new FileRunStateStore(root);
            var path = Path.Combine(root, $"{runId}.json");

            await File.WriteAllTextAsync(path, "{ broken json");

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new SuccessStep());

            var ex = await Assert.ThrowsAsync<InvalidDataException>(() =>
                new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId));

            Assert.Contains("malformed JSON", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    private static WorkflowDefinition BuildLinearWorkflow()
        => new()
        {
            Name = "linear",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = "a", Type = "test.ok" },
                                new StepDefinition { Step = "b", Type = "test.check", DependsOn = { "a" } },
                                new StepDefinition { Step = "c", Type = "test.ok", DependsOn = { "b" } }
                            }
                        }
                    }
                }
            }
        };

    private static WorkflowDefinition BuildMultiStageWorkflow()
        => new()
        {
            Name = "multi",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job1",
                            Steps =
                            {
                                new StepDefinition { Step = "s1a", Type = "test.ok" },
                                new StepDefinition { Step = "s1b", Type = "test.ok", DependsOn = { "s1a" } }
                            }
                        }
                    }
                },
                new StageDefinition
                {
                    Stage = "stage2",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job2",
                            Steps =
                            {
                                new StepDefinition { Step = "s2a", Type = "test.ok" },
                                new StepDefinition { Step = "s2b", Type = "test.fail.once", DependsOn = { "s2a" } },
                                new StepDefinition { Step = "s2c", Type = "test.ok", DependsOn = { "s2b" } }
                            }
                        }
                    }
                }
            }
        };

    private static WorkflowDefinition BuildSingleStepWorkflow(string stepId, string type)
        => new()
        {
            Name = "single",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "stage",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job",
                            Steps =
                            {
                                new StepDefinition { Step = stepId, Type = type }
                            }
                        }
                    }
                }
            }
        };

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class SuccessStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class RecordingStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(context.StepId);
            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["done"] = true }
            });
        }
    }

    private sealed class FailOnceByStepStep(Dictionary<string, int> attempts, string stepToFail, List<string>? executed = null) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed?.Add(context.StepId);
            attempts.TryGetValue(context.StepId, out var count);
            attempts[context.StepId] = count + 1;

            if (string.Equals(context.StepId, stepToFail, StringComparison.OrdinalIgnoreCase)
                && attempts[context.StepId] == 1)
            {
                return Task.FromResult(new StepResult { Success = false, Error = "fail once" });
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class RequireVariableStep(string key, object expectedValue) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (!context.Variables.TryGetValue(key, out var value) || !Equals(value, expectedValue))
            {
                return Task.FromResult(new StepResult
                {
                    Success = false,
                    Error = $"Missing or incorrect variable '{key}'."
                });
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class ThrowIfExecutedStep(string reason) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => throw new InvalidOperationException(reason);
    }

    private sealed class ThrowingStep(string message) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => throw new InvalidOperationException(message);
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}

