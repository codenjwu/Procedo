using Procedo.Core.Models;
using Procedo.Engine.Hosting;
using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public sealed class PluginRegistrationModesTests
{
    [Fact]
    public async Task Register_With_Delegate_Handler_Should_Resolve_And_Execute()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("custom.delegate", context => new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["greeting"] = $"Hello, {context.Inputs["name"]}"
            }
        });

        var resolved = registry.TryResolve("custom.delegate", out var step);

        Assert.True(resolved);
        Assert.NotNull(step);

        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["name"] = "Procedo"
            },
            Logger = new ConsoleLogger()
        });

        Assert.True(result.Success);
        Assert.Equal("Hello, Procedo", result.Outputs["greeting"]);
    }

    [Fact]
    public async Task Register_Generic_Step_Should_Use_ServiceProvider_For_Constructor_Injection()
    {
        IPluginRegistry registry = new PluginRegistry()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("Injected")
            }));

        registry.Register<InjectedStep>("custom.di");

        var resolved = registry.TryResolve("custom.di", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["name"] = "Procedo"
            },
            Logger = new ConsoleLogger()
        });

        Assert.True(result.Success);
        Assert.Equal("Injected Procedo", result.Outputs["message"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Bind_Inputs_And_Services_And_Convert_Result()
    {
        IPluginRegistry registry = new PluginRegistry()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("Method")
            }));

        registry.RegisterMethod("custom.method", (Func<string, GreetingService, CancellationToken, MethodPayload>)BuildPayload);

        var resolved = registry.TryResolve("custom.method", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["name"] = "Procedo"
            },
            Logger = new ConsoleLogger(),
            CancellationToken = CancellationToken.None
        });

        Assert.True(result.Success);
        Assert.Equal("Method Procedo", result.Outputs["Message"]);
        Assert.Equal(14, result.Outputs["Length"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Support_Async_Scalar_Returns()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method.async", (Func<string, CancellationToken, Task<string>>)BuildMessageAsync);

        var resolved = registry.TryResolve("custom.method.async", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["name"] = "Procedo"
            },
            Logger = new ConsoleLogger(),
            CancellationToken = CancellationToken.None
        });

        Assert.True(result.Success);
        Assert.Equal("Async Procedo", result.Outputs["value"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Support_Input_Aliases_And_Flat_Object_Binding()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method.alias", (Func<string, PublishOptions, PublishPayload>)BuildAliasedPayload);

        var resolved = registry.TryResolve("custom.method.alias", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["user_name"] = "Procedo",
                ["environment"] = "prod",
                ["retryCount"] = 3
            },
            Logger = new ConsoleLogger()
        });

        Assert.True(result.Success);
        Assert.Equal("Procedo-prod-3", result.Outputs["Summary"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Support_Nested_Object_Binding()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method.nested", (Func<PublishOptions, PublishPayload>)BuildNestedPayload);

        var resolved = registry.TryResolve("custom.method.nested", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["publishOptions"] = new Dictionary<string, object>
                {
                    ["environment"] = "stage",
                    ["retryCount"] = 5
                }
            },
            Logger = new ConsoleLogger()
        });

        Assert.True(result.Success);
        Assert.Equal("stage-5", result.Outputs["Summary"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Support_Explicit_Source_Attributes()
    {
        IPluginRegistry registry = new PluginRegistry()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("Service")
            }));

        registry.RegisterMethod("custom.method.sources", (Func<StepContext, GreetingService, ILogger, CancellationToken, SourcePayload>)BuildFromSources);

        var resolved = registry.TryResolve("custom.method.sources", out var step);

        Assert.True(resolved);
        var result = await step!.ExecuteAsync(new StepContext
        {
            StepId = "step-a",
            Inputs = new Dictionary<string, object>(),
            Logger = new ConsoleLogger(),
            CancellationToken = CancellationToken.None
        });

        Assert.True(result.Success);
        Assert.Equal("step-a-Service ok", result.Outputs["Summary"]);
    }

    [Fact]
    public async Task RegisterMethod_Should_Produce_Clear_Binding_Error_Message()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method.error", (Func<string, int, string>)BuildStrictSummary);

        registry.TryResolve("custom.method.error", out var step);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["name"] = "Procedo"
            },
            Logger = new ConsoleLogger()
        }));

        Assert.Contains("Unable to bind parameter 'attempts'", ex.Message);
        Assert.Contains("Available inputs: name", ex.Message);
    }

    [Fact]
    public async Task RegisterMethod_Should_Reject_Invalid_Explicit_Source_Attribute_Usage()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method.badattr", (Func<string, string>)BuildInvalidAttributeUsage);

        registry.TryResolve("custom.method.badattr", out var step);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>(),
            Logger = new ConsoleLogger()
        }));

        Assert.Contains("[FromLogger] can only be used with ILogger parameters", ex.Message);
    }

    [Fact]
    public void TryRegister_With_Delegate_Should_Return_False_For_Duplicate_Key()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("custom.delegate", _ => new StepResult { Success = true, Outputs = new Dictionary<string, object> { ["value"] = "first" } });

        var added = registry.TryRegister("custom.delegate", _ => new StepResult { Success = true, Outputs = new Dictionary<string, object> { ["value"] = "second" } });

        Assert.False(added);
    }

    [Fact]
    public void RegisterMethodOrThrow_Should_Throw_For_Duplicate_Key()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.RegisterMethod("custom.method", (Func<string, CancellationToken, Task<string>>)BuildMessageAsync);

        Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterMethodOrThrow("custom.method", (Func<string, CancellationToken, Task<string>>)BuildMessageAsync));
    }

    [Fact]
    public void TryRegister_Generic_Step_Should_Return_False_For_Duplicate_Key()
    {
        IPluginRegistry registry = new PluginRegistry()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("Injected")
            }));

        registry.Register<InjectedStep>("custom.di");

        var added = registry.TryRegister<InjectedStep>("custom.di");

        Assert.False(added);
    }

    [Fact]
    public async Task HostBuilder_UseServiceProvider_Should_Enable_Injected_Steps()
    {
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
                                }
                            }
                        }
                    }
                }
            }
        };

        var host = new ProcedoHostBuilder()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("Hosted")
            }))
            .ConfigurePlugins(static registry => registry.Register<InjectedStep>("custom.di"))
            .Build();

        var result = await host.ExecuteWorkflowAsync(workflow);

        Assert.True(result.Success);
    }

    private static MethodPayload BuildPayload(string name, GreetingService greetingService, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var message = greetingService.Create(name);
        return new MethodPayload(message, message.Length);
    }

    private static Task<string> BuildMessageAsync(string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult($"Async {name}");
    }

    private static PublishPayload BuildAliasedPayload([StepInput("user_name")] string name, PublishOptions options)
        => new($"{name}-{options.Environment}-{options.RetryCount}");

    private static PublishPayload BuildNestedPayload(PublishOptions publishOptions)
        => new($"{publishOptions.Environment}-{publishOptions.RetryCount}");

    private static SourcePayload BuildFromSources(
        [FromStepContext] StepContext context,
        [FromServices] GreetingService greetingService,
        [FromLogger] ILogger logger,
        [FromCancellationToken] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("source payload created");
        return new SourcePayload($"{context.StepId}-{greetingService.Create("ok")}");
    }

    private static string BuildStrictSummary(string name, int attempts)
        => $"{name}:{attempts}";

    private static string BuildInvalidAttributeUsage([FromLogger] string text)
        => text;

    private sealed record MethodPayload(string Message, int Length);
    private sealed record PublishPayload(string Summary);
    private sealed record SourcePayload(string Summary);

    private sealed class PublishOptions
    {
        public string Environment { get; set; } = string.Empty;

        public int RetryCount { get; set; }
    }

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

    private sealed class DictionaryServiceProvider : IServiceProvider
    {
        private readonly IReadOnlyDictionary<Type, object> _services;

        public DictionaryServiceProvider(IReadOnlyDictionary<Type, object> services)
        {
            _services = services;
        }

        public object? GetService(Type serviceType)
            => _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
