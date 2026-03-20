using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowObservabilityPersistenceIntegrationTests
{
    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Emit_Resumed_Run_And_Replayed_StepCompleted()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "obs_resume",
                Stages =
                {
                    new StageDefinition
                    {
                        Stage = "s1",
                        Jobs =
                        {
                            new JobDefinition
                            {
                                Job = "j1",
                                Steps =
                                {
                                    new StepDefinition { Step = "a", Type = "test.ok" },
                                    new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" } }
                                }
                            }
                        }
                    }
                }
            };

            var store = new FileRunStateStore(root);
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new SuccessStep());

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(first.Success);

            sink.Events.Clear();

            var second = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(second.Success);

            Assert.Equal(ExecutionEventType.RunStarted, sink.Events[0].EventType);
            Assert.True(sink.Events[0].Resumed);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "a" && e.Resumed == true);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "b" && e.Resumed == true);
            Assert.Equal(ExecutionEventType.RunCompleted, sink.Events[^1].EventType);
            Assert.True(sink.Events[^1].Resumed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Distinguish_Replayed_Completions_From_Skipped_Steps()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "obs_resume_skip_distinction",
                Variables =
                {
                    ["vars.shouldRun"] = false
                },
                Stages =
                {
                    new StageDefinition
                    {
                        Stage = "s1",
                        Jobs =
                        {
                            new JobDefinition
                            {
                                Job = "j1",
                                Steps =
                                {
                                    new StepDefinition { Step = "a", Type = "test.ok" },
                                    new StepDefinition { Step = "b", Type = "test.ok", DependsOn = { "a" }, Condition = "eq(vars.shouldRun, true)" }
                                }
                            }
                        }
                    }
                }
            };

            var store = new FileRunStateStore(root);
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new SuccessStep());

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(first.Success);

            sink.Events.Clear();

            var second = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(second.Success);

            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "a" && e.Resumed == true);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "b" && e.Resumed == true);
            Assert.DoesNotContain(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped && e.StepId == "a");
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

    private sealed class InMemorySink : IExecutionEventSink
    {
        public List<ExecutionEvent> Events { get; } = new();

        public Task WriteAsync(ExecutionEvent executionEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(executionEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}
