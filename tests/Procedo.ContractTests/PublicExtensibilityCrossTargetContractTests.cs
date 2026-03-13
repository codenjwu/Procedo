using Microsoft.Extensions.DependencyInjection;
using Procedo.Plugin.SDK;

namespace Procedo.ContractTests;

public sealed class PublicExtensibilityCrossTargetContractTests
{
    [Fact]
    public void StepContext_Defaults_Should_Be_Usable()
    {
        var context = new StepContext();

        Assert.Equal(string.Empty, context.RunId);
        Assert.Equal(string.Empty, context.StepId);
        Assert.NotNull(context.Inputs);
        Assert.NotNull(context.Variables);
        Assert.NotNull(context.Logger);
        Assert.Equal(default, context.CancellationToken);
    }

    [Fact]
    public void StepResult_Defaults_Should_Be_Usable()
    {
        var result = new StepResult();

        Assert.False(result.Success);
        Assert.False(result.Waiting);
        Assert.NotNull(result.Outputs);
        Assert.Empty(result.Outputs);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task PluginRegistry_Should_Be_CaseInsensitive_And_LastRegistration_Wins()
    {
        var registry = new PluginRegistry();
        registry.Register("custom.echo", () => new ConstantStep("first"));
        registry.Register("CUSTOM.ECHO", () => new ConstantStep("second"));

        var resolved = registry.TryResolve("Custom.Echo", out var step);

        Assert.True(resolved);
        Assert.NotNull(step);

        var result = await step!.ExecuteAsync(new StepContext());
        Assert.Equal("second", result.Outputs["value"]);
    }

    [Fact]
    public async Task PluginRegistry_TryRegister_Should_Preserve_First_Registration()
    {
        var registry = new PluginRegistry();

        var first = registry.TryRegister("custom.try", () => new ConstantStep("first"));
        var second = registry.TryRegister("CUSTOM.TRY", () => new ConstantStep("second"));

        Assert.True(first);
        Assert.False(second);

        Assert.True(registry.TryResolve("custom.try", out var step));
        var result = await step!.ExecuteAsync(new StepContext());
        Assert.Equal("first", result.Outputs["value"]);
    }

    [Fact]
    public void PluginRegistry_RegisterOrThrow_Should_Reject_Duplicate_Registration()
    {
        var registry = new PluginRegistry();
        registry.RegisterOrThrow("custom.dup", () => new ConstantStep("first"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            registry.RegisterOrThrow("CUSTOM.DUP", () => new ConstantStep("second")));

        Assert.Contains("already registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DelegateRegistration_Should_Execute_Handler_And_Return_Outputs()
    {
        var registry = new PluginRegistry();
        registry.Register("custom.delegate", context => new StepResult
        {
            Success = true,
            Outputs = new Dictionary<string, object>
            {
                ["message"] = $"hello {context.StepId}"
            }
        });

        Assert.True(registry.TryResolve("custom.delegate", out var step));
        var result = await step!.ExecuteAsync(new StepContext { StepId = "delegate" });

        Assert.True(result.Success);
        Assert.Equal("hello delegate", result.Outputs["message"]);
    }

    [Fact]
    public async Task GenericRegistration_Should_Resolve_Step_From_ServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new GreetingService("hello"));
        services.AddTransient<GreetingStep>();

        using var provider = services.BuildServiceProvider();

        var registry = new PluginRegistry();
        registry.UseServiceProvider(provider);
        registry.Register<GreetingStep>("custom.di");

        Assert.True(registry.TryResolve("custom.di", out var step));
        var result = await step!.ExecuteAsync(new StepContext { StepId = "world" });

        Assert.True(result.Success);
        Assert.Equal("hello world", result.Outputs["message"]);
    }

    [Fact]
    public async Task MethodRegistration_Should_Bind_Simple_Input_And_Scalar_Return()
    {
        var registry = new PluginRegistry();
        registry.RegisterMethod("custom.method", (Func<string, string>)FormatMessage);

        Assert.True(registry.TryResolve("custom.method", out var step));
        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["value"] = "procedo"
            }
        });

        Assert.True(result.Success);
        Assert.Equal("echo:procedo", result.Outputs["value"]);
    }

    private static string FormatMessage(string value) => $"echo:{value}";

    private sealed class ConstantStep : IProcedoStep
    {
        private readonly string _value;

        public ConstantStep(string value)
        {
            _value = value;
        }

        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["value"] = _value
                }
            });
    }

    private sealed class GreetingService
    {
        public GreetingService(string prefix)
        {
            Prefix = prefix;
        }

        public string Prefix { get; }
    }

    private sealed class GreetingStep : IProcedoStep
    {
        private readonly GreetingService _service;

        public GreetingStep(GreetingService service)
        {
            _service = service;
        }

        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object>
                {
                    ["message"] = $"{_service.Prefix} {context.StepId}"
                }
            });
    }
}
