using Microsoft.Extensions.DependencyInjection;
using Procedo.Core.Models;
using Procedo.Engine.Hosting;
using Procedo.Extensions.DependencyInjection;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public sealed class ProcedoDependencyInjectionTests
{
    [Fact]
    public async Task AddProcedo_Should_Register_ProcedoHost_And_Execute_Injected_Step()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new GreetingService("DI"));
        services.AddProcedo()
            .RegisterStep<InjectedStep>("custom.di")
            .RegisterStep("custom.delegate", static context => new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = context.Inputs["name"]
                }
            });

        using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<ProcedoHost>();

        var workflow = new WorkflowDefinition
        {
            Name = "di_host",
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
                                    Step = "a",
                                    Type = "custom.di",
                                    With = new Dictionary<string, object>
                                    {
                                        ["name"] = "Procedo"
                                    }
                                },
                                new StepDefinition
                                {
                                    Step = "b",
                                    Type = "custom.delegate",
                                    DependsOn = new List<string> { "a" },
                                    With = new Dictionary<string, object>
                                    {
                                        ["name"] = "ok"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = await host.ExecuteWorkflowAsync(workflow);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task AddProcedo_Should_Register_Method_Bound_Step()
    {
        var services = new ServiceCollection();
        services.AddProcedo()
            .RegisterMethod("custom.summary", (Func<string, SummaryPayload>)BuildSummary);

        using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<ProcedoHost>();

        var workflow = new WorkflowDefinition
        {
            Name = "method_host",
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
                                    Step = "a",
                                    Type = "custom.summary",
                                    With = new Dictionary<string, object>
                                    {
                                        ["name"] = "Procedo"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        var result = await host.ExecuteWorkflowAsync(workflow);

        Assert.True(result.Success);
    }

    [Fact]
    public void AddProcedo_Should_Return_Same_Builder_Instance_When_Called_Twice()
    {
        var services = new ServiceCollection();

        var first = services.AddProcedo();
        var second = services.AddProcedo();

        Assert.Same(first, second);
    }

    private static SummaryPayload BuildSummary(string name) => new($"Hello, {name}");

    private sealed record SummaryPayload(string Message);

    private sealed class GreetingService
    {
        private readonly string _prefix;

        public GreetingService(string prefix)
        {
            _prefix = prefix;
        }

        public string Create(string name) => $"{_prefix} {name}";
    }

    private sealed class InjectedStep : IProcedoStep
    {
        private readonly GreetingService _greetingService;

        public InjectedStep(GreetingService greetingService)
        {
            _greetingService = greetingService;
        }

        public Task<StepResult> ExecuteAsync(StepContext context)
        {
            var name = context.Inputs.TryGetValue("name", out var value)
                ? value?.ToString() ?? "world"
                : "world";

            return Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["message"] = _greetingService.Create(name)
                }
            });
        }
    }
}
