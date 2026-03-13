using Procedo.Core.Models;
using Procedo.Engine.Hosting;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class ProcedoHostBuilderTests
{
    [Fact]
    public async Task Build_Should_Execute_Workflow_With_Configured_Options()
    {
        var workflow = new WorkflowDefinition
        {
            Name = "host_builder_ok",
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
                                new StepDefinition { Step = "a", Type = "test.ok" }
                            }
                        }
                    }
                }
            }
        };

        var host = new ProcedoHostBuilder()
            .ConfigurePlugins(static registry => registry.Register("test.ok", () => new OkStep()))
            .ConfigureExecution(static execution =>
            {
                execution.DefaultMaxParallelism = 2;
                execution.DefaultStepRetries = 1;
            })
            .Build();

        var result = await host.ExecuteWorkflowAsync(workflow);

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.RunId));
    }

    [Fact]
    public async Task Build_Should_Throw_Validation_Exception_When_Workflow_Has_Errors()
    {
        var invalidWorkflow = new WorkflowDefinition
        {
            Name = "invalid",
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
                                new StepDefinition { Step = "dup", Type = "test.ok" },
                                new StepDefinition { Step = "dup", Type = "test.ok" }
                            }
                        }
                    }
                }
            }
        };

        var host = new ProcedoHostBuilder()
            .ConfigurePlugins(static registry => registry.Register("test.ok", () => new OkStep()))
            .Build();

        var ex = await Assert.ThrowsAsync<ProcedoValidationException>(() => host.ExecuteWorkflowAsync(invalidWorkflow));
        Assert.True(ex.ValidationResult.HasErrors);
    }

    [Fact]
    public void Configure_Should_Throw_For_Null_Action()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.Configure(null!));
    }

    [Fact]
    public void ConfigurePlugins_Should_Throw_For_Null_Action()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.ConfigurePlugins(null!));
    }

    [Fact]
    public void ConfigureExecution_Should_Throw_For_Null_Action()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.ConfigureExecution(null!));
    }

    [Fact]
    public void ConfigureValidation_Should_Throw_For_Null_Action()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.ConfigureValidation(null!));
    }

    [Fact]
    public void UseLogger_Should_Throw_For_Null_Logger()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.UseLogger(null!));
    }

    [Fact]
    public void UseServiceProvider_Should_Throw_For_Null_ServiceProvider()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.UseServiceProvider(null!));
    }

    [Fact]
    public void UseRunStateStore_Should_Throw_For_Null_Store()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.UseRunStateStore(null!));
    }

    [Fact]
    public void UseLocalRunStateStore_Should_Throw_For_Empty_Path()
    {
        var builder = new ProcedoHostBuilder();
        Assert.Throws<ArgumentException>(() => builder.UseLocalRunStateStore(""));
    }

    [Fact]
    public void Build_Should_Throw_When_Resume_RunId_Is_Configured_Without_RunStateStore()
    {
        var builder = new ProcedoHostBuilder()
            .Configure(options => options.ResumeRunId = "run-123");

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("run state store", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_Should_Throw_When_Parser_Is_Null()
    {
        var builder = new ProcedoHostBuilder()
            .Configure(options => options.Parser = null!);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("parser", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
    private sealed class OkStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }
}

