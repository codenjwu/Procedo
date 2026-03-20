using System.Collections.Concurrent;
using System.Threading;
using Procedo.Core.Execution;
using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Persistence.Stores;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class WorkflowExecutionPolicyIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Retry_Flaky_Step_And_Succeed()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
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
                                new StepDefinition { Step = "a", Type = "test.flaky", Retries = 1 }
                            }
                        }
                    }
                }
            }
        };

        FlakyStep.Reset();
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.flaky", () => new FlakyStep());

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(
            workflow,
            registry,
            new NullLogger(),
            null,
            default,
            new WorkflowExecutionOptions { RetryInitialBackoffMs = 1, RetryMaxBackoffMs = 2 });

        Assert.True(result.Success);
        Assert.Equal(2, FlakyStep.Attempts);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Run_Siblings_When_ContinueOnError_Is_Enabled()
    {
        var executed = new ConcurrentBag<string>();
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            ContinueOnError = true,
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
                                new StepDefinition { Step = "fail", Type = "test.fail" },
                                new StepDefinition { Step = "ok", Type = "test.ok" }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.fail", () => new FailStep(executed));
        registry.Register("test.ok", () => new OkStep(executed));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.False(result.Success);
        Assert.Contains("ok", executed);
        Assert.Contains("fail", executed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Honor_MaxParallelism_From_Workflow()
    {
        var tracker = new ConcurrencyTracker();
        var workflow = new WorkflowDefinition
        {
            Name = "wf",
            MaxParallelism = 2,
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
                                new StepDefinition { Step = "a", Type = "test.concurrent" },
                                new StepDefinition { Step = "b", Type = "test.concurrent" }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.concurrent", () => new ConcurrentStep(tracker));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.True(result.Success);
        Assert.True(tracker.MaxSeen >= 2, $"Expected max concurrency >= 2, got {tracker.MaxSeen}");
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Retry_Flaky_Step_And_Succeed()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = new WorkflowDefinition
            {
                Name = "wf",
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
                                    new StepDefinition { Step = "a", Type = "test.flaky", Retries = 1 }
                                }
                            }
                        }
                    }
                }
            };

            FlakyStep.Reset();
            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.flaky", () => new FlakyStep());

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
                workflow,
                registry,
                new NullLogger(),
                new FileRunStateStore(root),
                "persisted-retry-run",
                null,
                default,
                new WorkflowExecutionOptions { RetryInitialBackoffMs = 1, RetryMaxBackoffMs = 2 });

            Assert.True(result.Success);
            Assert.Equal(2, FlakyStep.Attempts);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Run_Siblings_When_ContinueOnError_Is_Enabled()
    {
        var root = CreateTempDirectory();
        try
        {
            var executed = new ConcurrentBag<string>();
            var workflow = new WorkflowDefinition
            {
                Name = "wf",
                ContinueOnError = true,
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
                                    new StepDefinition { Step = "fail", Type = "test.fail" },
                                    new StepDefinition { Step = "ok", Type = "test.ok" }
                                }
                            }
                        }
                    }
                }
            };

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.fail", () => new FailStep(executed));
            registry.Register("test.ok", () => new OkStep(executed));

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
                workflow,
                registry,
                new NullLogger(),
                new FileRunStateStore(root),
                "persisted-coe-run");

            Assert.False(result.Success);
            Assert.Contains("ok", executed);
            Assert.Contains("fail", executed);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Honor_MaxParallelism_From_Workflow()
    {
        var root = CreateTempDirectory();
        try
        {
            var tracker = new ConcurrencyTracker();
            var workflow = new WorkflowDefinition
            {
                Name = "wf",
                MaxParallelism = 2,
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
                                    new StepDefinition { Step = "a", Type = "test.concurrent" },
                                    new StepDefinition { Step = "b", Type = "test.concurrent" }
                                }
                            }
                        }
                    }
                }
            };

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.concurrent", () => new ConcurrentStep(tracker));

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
                workflow,
                registry,
                new NullLogger(),
                new FileRunStateStore(root),
                "persisted-parallel-run");

            Assert.True(result.Success);
            Assert.True(tracker.MaxSeen >= 2, $"Expected max concurrency >= 2, got {tracker.MaxSeen}");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ExecuteWithPersistenceAsync_Should_Honor_Default_Timeout()
    {
        var root = CreateTempDirectory();
        try
        {
            var workflow = new WorkflowDefinition
            {
                Name = "wf",
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
                                    new StepDefinition { Step = "slow", Type = "test.slow" }
                                }
                            }
                        }
                    }
                }
            };

            IPluginRegistry registry = new PluginRegistry();
            registry.Register("test.slow", () => new SlowStep());

            var result = await new ProcedoWorkflowEngine().ExecuteWithPersistenceAsync(
                workflow,
                registry,
                new NullLogger(),
                new FileRunStateStore(root),
                "persisted-timeout-run",
                null,
                default,
                new WorkflowExecutionOptions { DefaultStepTimeoutMs = 10 });

            Assert.False(result.Success);
            Assert.Equal(RuntimeErrorCodes.StepTimeout, result.ErrorCode);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FlakyStep : IProcedoStep
    {
        public static int Attempts;

        public static void Reset() => Attempts = 0;

        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            Attempts++;
            if (Attempts == 1)
            {
                return Task.FromResult(new StepResult { Success = false, Error = "first failure" });
            }

            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class FailStep(ConcurrentBag<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add("fail");
            return Task.FromResult(new StepResult { Success = false, Error = "fail" });
        }
    }

    private sealed class OkStep(ConcurrentBag<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add("ok");
            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class ConcurrentStep(ConcurrencyTracker tracker) : IProcedoStep
    {
        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            tracker.Enter();
            try
            {
                await Task.Delay(100, context.CancellationToken);
                return new StepResult { Success = true };
            }
            finally
            {
                tracker.Exit();
            }
        }
    }

    private sealed class SlowStep : IProcedoStep
    {
        public async Task<StepResult> ExecuteAsync(StepContext context)
        {
            await Task.Delay(100, context.CancellationToken);
            return new StepResult { Success = true };
        }
    }

    private sealed class ConcurrencyTracker
    {
        private int _current;
        private int _maxSeen;

        public int MaxSeen => _maxSeen;

        public void Enter()
        {
            var next = Interlocked.Increment(ref _current);
            while (true)
            {
                var observed = Volatile.Read(ref _maxSeen);
                if (next <= observed)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _maxSeen, next, observed) == observed)
                {
                    return;
                }
            }
        }

        public void Exit() => Interlocked.Decrement(ref _current);
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "procedo-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}


