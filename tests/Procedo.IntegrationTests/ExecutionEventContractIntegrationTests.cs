using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Observability;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;
using Procedo.Plugin.System;

namespace Procedo.IntegrationTests;

public class ExecutionEventContractIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Emit_Contract_Compliant_Events_For_Success_Path()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "contract_success",
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

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new SuccessStep());

        var sink = new InMemorySink();
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink);

        Assert.True(result.Success);
        Assert.NotEmpty(sink.Events);
        AssertMonotonicSequence(sink.Events);
        Assert.All(sink.Events, AssertContract);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Emit_Contract_Compliant_Events_For_Failure_Path()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "contract_failure",
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
                                new StepDefinition { Step = "a", Type = "test.fail" }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep());

        var sink = new InMemorySink();
        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), sink);

        Assert.False(result.Success);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepFailed);
        Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunFailed);
        AssertMonotonicSequence(sink.Events);
        Assert.All(sink.Events, AssertContract);
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Emit_Contract_Compliant_Events_For_Resume_Path()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "contract_resume",
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

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.ok", () => new SuccessStep());

            var store = new FileRunStateStore(root);
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(first.Success);

            sink.Events.Clear();
            var resumed = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(resumed.Success);

            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepSkipped);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunStarted && e.Resumed == true);
            AssertMonotonicSequence(sink.Events);
            Assert.All(sink.Events, AssertContract);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ResumeAsync_Should_Emit_Contract_Compliant_Waiting_And_Resume_Events()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "contract_wait_resume",
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
                                    new StepDefinition { Step = "wait", Type = "test.wait" },
                                    new StepDefinition { Step = "after", Type = "test.ok", DependsOn = { "wait" } }
                                }
                            }
                        }
                    }
                }
            };

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.wait", () => new WaitUntilResumedStep());
            registry.Register("test.ok", () => new SuccessStep());

            var store = new FileRunStateStore(root);
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(first.Waiting);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.StepWaiting && e.WaitType == "signal");
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunWaiting && e.WaitType == "signal");
            AssertMonotonicSequence(sink.Events);
            Assert.All(sink.Events, AssertContract);

            sink.Events.Clear();
            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                runId,
                new Procedo.Core.Runtime.ResumeRequest { SignalType = "continue" },
                sink);

            Assert.True(resumed.Success);
            Assert.Contains(sink.Events, e => e.EventType == ExecutionEventType.RunResumed && e.SignalType == "continue");
            AssertMonotonicSequence(sink.Events);
            Assert.All(sink.Events, AssertContract);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public async Task ResumeAsync_Should_Redact_Resume_Payload_In_Observed_Step_Outputs()
    {
        var root = CreateTempDirectory();
        try
        {
            var runId = Guid.NewGuid().ToString("N");
            var workflow = new WorkflowDefinition
            {
                Name = "contract_wait_signal_redaction",
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
                                    new StepDefinition
                                    {
                                        Step = "wait",
                                        Type = "system.wait_signal",
                                        With =
                                        {
                                            ["signal_type"] = "continue"
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            IPluginRegistry registry = new PluginRegistry();
            registry.AddSystemPlugin();

            var store = new FileRunStateStore(root);
            var sink = new InMemorySink();
            var engine = new ProcedoWorkflowEngine();

            var first = await engine.ExecuteWithPersistenceAsync(workflow, registry, new NullLogger(), store, runId, sink);
            Assert.True(first.Waiting);

            sink.Events.Clear();
            var resumed = await engine.ResumeAsync(
                workflow,
                registry,
                new NullLogger(),
                store,
                runId,
                new Procedo.Core.Runtime.ResumeRequest
                {
                    SignalType = "continue",
                    Payload = new Dictionary<string, object>
                    {
                        ["approved_by"] = "operator",
                        ["token"] = "secret-token"
                    }
                },
                sink);

            Assert.True(resumed.Success);
            var stepCompleted = Assert.Single(sink.Events.Where(e => e.EventType == ExecutionEventType.StepCompleted && e.StepId == "wait"));
            var outputs = Assert.IsType<Dictionary<string, object>>(stepCompleted.Outputs);
            var payload = Assert.IsType<Dictionary<string, object>>(outputs["payload"]);

            Assert.Equal("***REDACTED***", payload["approved_by"]);
            Assert.Equal("***REDACTED***", payload["token"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
    private static void AssertMonotonicSequence(IReadOnlyList<ExecutionEvent> events)
    {
        Assert.NotEmpty(events);

        for (var i = 0; i < events.Count; i++)
        {
            Assert.Equal(i + 1, events[i].Sequence);
            Assert.True(events[i].TimestampUtc > DateTimeOffset.MinValue);
        }
    }

    private static void AssertContract(ExecutionEvent e)
    {
        Assert.False(string.IsNullOrWhiteSpace(e.RunId));
        Assert.False(string.IsNullOrWhiteSpace(e.WorkflowName));

        switch (e.EventType)
        {
            case ExecutionEventType.RunStarted:
                Assert.NotNull(e.Resumed);
                Assert.Null(e.Stage);
                Assert.Null(e.Job);
                Assert.Null(e.StepId);
                break;

            case ExecutionEventType.RunCompleted:
                Assert.True(e.Success);
                Assert.NotNull(e.DurationMs);
                Assert.True(e.DurationMs >= 0);
                break;

            case ExecutionEventType.RunFailed:
                Assert.False(e.Success);
                Assert.NotNull(e.DurationMs);
                Assert.True(e.DurationMs >= 0);
                Assert.False(string.IsNullOrWhiteSpace(e.Error));
                break;

            case ExecutionEventType.StepStarted:
                AssertStepScope(e);
                Assert.Null(e.Success);
                break;

            case ExecutionEventType.StepCompleted:
                AssertStepScope(e);
                Assert.True(e.Success);
                Assert.NotNull(e.DurationMs);
                Assert.True(e.DurationMs >= 0);
                break;

            case ExecutionEventType.StepFailed:
                AssertStepScope(e);
                Assert.False(e.Success);
                Assert.False(string.IsNullOrWhiteSpace(e.Error));
                break;

            case ExecutionEventType.StepSkipped:
                AssertStepScope(e);
                Assert.True(e.Success);
                Assert.True(e.Resumed);
                break;

            case ExecutionEventType.StepWaiting:
                AssertStepScope(e);
                Assert.NotNull(e.DurationMs);
                Assert.False(string.IsNullOrWhiteSpace(e.WaitType));
                break;

            case ExecutionEventType.RunWaiting:
                Assert.NotNull(e.DurationMs);
                Assert.False(string.IsNullOrWhiteSpace(e.WaitType));
                break;

            case ExecutionEventType.RunResumed:
                Assert.True(e.Resumed);
                Assert.False(string.IsNullOrWhiteSpace(e.SignalType));
                Assert.Null(e.Stage);
                Assert.Null(e.Job);
                Assert.Null(e.StepId);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static void AssertStepScope(ExecutionEvent e)
    {
        Assert.False(string.IsNullOrWhiteSpace(e.Stage));
        Assert.False(string.IsNullOrWhiteSpace(e.Job));
        Assert.False(string.IsNullOrWhiteSpace(e.StepId));
        Assert.False(string.IsNullOrWhiteSpace(e.StepType));
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

    private sealed class FailStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = false, Error = "failed" });
    }

    private sealed class WaitUntilResumedStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            if (string.Equals(context.Resume?.SignalType, "continue", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new StepResult { Success = true });
            }

            return Task.FromResult(new StepResult
            {
                Waiting = true,
                Wait = new Procedo.Core.Runtime.WaitDescriptor
                {
                    Type = "signal",
                    Reason = "waiting",
                    Key = context.RunId
                }
            });
        }
    }
    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}






