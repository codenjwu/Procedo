using Procedo.Core.Models;
using Procedo.Engine;
using Procedo.Plugin.SDK;

namespace Procedo.IntegrationTests;

public class ProcedoWorkflowEngineAdvancedIntegrationTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Respect_Partial_Order_In_Complex_Dag()
    {
        var executed = new List<string>();
        var workflow = BuildComplexDagWorkflow();

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.dag", () => new DagStep(executed));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.True(result.Success);

        AssertBefore(executed, "extract_users", "normalize_users");
        AssertBefore(executed, "extract_orders", "normalize_orders");
        AssertBefore(executed, "normalize_users", "join_sales");
        AssertBefore(executed, "normalize_orders", "join_sales");
        AssertBefore(executed, "join_sales", "score_risk");
        AssertBefore(executed, "extract_inventory", "score_risk");
        AssertBefore(executed, "score_risk", "publish");
    }

    [Fact]
    public async Task ExecuteAsync_Should_Stop_Branch_When_Middle_Node_Fails()
    {
        var executed = new List<string>();

        var workflow = new WorkflowDefinition
        {
            Name = "branch_fail",
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
                                new StepDefinition { Step = "start", Type = "test.ok" },
                                new StepDefinition
                                {
                                    Step = "transform",
                                    Type = "test.fail",
                                    DependsOn = { "start" }
                                },
                                new StepDefinition
                                {
                                    Step = "publish",
                                    Type = "test.ok",
                                    DependsOn = { "transform" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new MarkerStep(executed, success: true));
        registry.Register("test.fail", () => new MarkerStep(executed, success: false));

        var result = await new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger());

        Assert.False(result.Success);
        Assert.Equal(["start", "transform"], executed);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Throw_When_Dependency_References_Step_From_Another_Job()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "cross_job_dep",
            Stages =
            {
                new StageDefinition
                {
                    Stage = "s1",
                    Jobs =
                    {
                        new JobDefinition
                        {
                            Job = "job_a",
                            Steps =
                            {
                                new StepDefinition { Step = "seed", Type = "test.ok" }
                            }
                        },
                        new JobDefinition
                        {
                            Job = "job_b",
                            Steps =
                            {
                                new StepDefinition
                                {
                                    Step = "consume",
                                    Type = "test.ok",
                                    DependsOn = { "seed" }
                                }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.ok", () => new MarkerStep(new List<string>(), true));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger()));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Cancel_When_Cancellation_Is_Triggered_During_A_Step()
    {
        var cts = new CancellationTokenSource();
        var executed = new List<string>();

        var workflow = new WorkflowDefinition
        {
            Name = "cancel_mid_run",
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
                                new StepDefinition { Step = "canceler", Type = "test.cancel" },
                                new StepDefinition { Step = "never", Type = "test.ok", DependsOn = { "canceler" } }
                            }
                        }
                    }
                }
            }
        };

        IPluginRegistry registry = new PluginRegistry();
        registry.Register("test.cancel", () => new CancelingStep(cts, executed));
        registry.Register("test.ok", () => new MarkerStep(executed, true));

        await Assert.ThrowsAsync<OperationCanceledException>(() => new ProcedoWorkflowEngine().ExecuteAsync(workflow, registry, new NullLogger(), cts.Token));
        Assert.Equal(["canceler"], executed);
    }

    private static WorkflowDefinition BuildComplexDagWorkflow() => new()
    {
        Name = "complex_dag",
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
                            new StepDefinition { Step = "extract_users", Type = "test.dag" },
                            new StepDefinition { Step = "extract_orders", Type = "test.dag" },
                            new StepDefinition { Step = "extract_inventory", Type = "test.dag" },
                            new StepDefinition { Step = "normalize_users", Type = "test.dag", DependsOn = { "extract_users" } },
                            new StepDefinition { Step = "normalize_orders", Type = "test.dag", DependsOn = { "extract_orders" } },
                            new StepDefinition { Step = "join_sales", Type = "test.dag", DependsOn = { "normalize_users", "normalize_orders" } },
                            new StepDefinition { Step = "score_risk", Type = "test.dag", DependsOn = { "join_sales", "extract_inventory" } },
                            new StepDefinition { Step = "publish", Type = "test.dag", DependsOn = { "score_risk" } }
                        }
                    }
                }
            }
        }
    };

    private static void AssertBefore(List<string> executed, string first, string second)
    {
        var i = executed.IndexOf(first);
        var j = executed.IndexOf(second);
        Assert.True(i >= 0 && j >= 0 && i < j, $"Expected '{first}' before '{second}', actual: {string.Join(",", executed)}");
    }

    private sealed class DagStep(List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(context.StepId);
            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["done"] = context.StepId }
            });
        }
    }

    private sealed class MarkerStep(List<string> executed, bool success) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(context.StepId);
            return Task.FromResult(new StepResult
            {
                Success = success,
                Error = success ? null : "intentional failure"
            });
        }
    }

    private sealed class CancelingStep(CancellationTokenSource cts, List<string> executed) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            executed.Add(context.StepId);
            cts.Cancel();
            return Task.FromResult(new StepResult { Success = true });
        }
    }

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }
        public void LogInformation(string message) { }
        public void LogWarning(string message) { }
    }
}

