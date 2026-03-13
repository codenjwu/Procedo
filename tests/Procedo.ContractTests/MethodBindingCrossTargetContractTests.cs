using Procedo.Plugin.SDK;

namespace Procedo.ContractTests;

public sealed class MethodBindingCrossTargetContractTests
{
    [Fact]
    public async Task MethodBinding_Should_Support_Aliases_And_Flat_Object_Binding()
    {
        var registry = new PluginRegistry();
        registry.RegisterMethod("custom.publish", (Func<string, PublishOptions, PublishPayload>)BuildPayload);

        Assert.True(registry.TryResolve("custom.publish", out var step));

        var result = await step!.ExecuteAsync(new StepContext
        {
            Inputs = new Dictionary<string, object>
            {
                ["user_name"] = "procedo",
                ["environment"] = "prod",
                ["retryCount"] = 2
            }
        });

        Assert.True(result.Success);
        Assert.Equal("procedo-prod-2", result.Outputs["Summary"]);
    }

    [Fact]
    public async Task MethodBinding_Should_Support_Explicit_Source_Attributes()
    {
        IPluginRegistry registry = new PluginRegistry()
            .UseServiceProvider(new DictionaryServiceProvider(new Dictionary<Type, object>
            {
                [typeof(GreetingService)] = new GreetingService("service")
            }));

        registry.RegisterMethod(
            "custom.sources",
            (Func<StepContext, GreetingService, ILogger, CancellationToken, SourcePayload>)BuildFromSources);

        Assert.True(registry.TryResolve("custom.sources", out var step));

        var result = await step!.ExecuteAsync(new StepContext
        {
            StepId = "step-a",
            Inputs = new Dictionary<string, object>(),
            Logger = new NullLogger(),
            CancellationToken = CancellationToken.None
        });

        Assert.True(result.Success);
        Assert.Equal("step-a-service ok", result.Outputs["Summary"]);
    }

    private static PublishPayload BuildPayload([StepInput("user_name")] string name, PublishOptions options)
        => new($"{name}-{options.Environment}-{options.RetryCount}");

    private static SourcePayload BuildFromSources(
        [FromStepContext] StepContext context,
        [FromServices] GreetingService greetingService,
        [FromLogger] ILogger logger,
        [FromCancellationToken] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("method binding executed");
        return new SourcePayload($"{context.StepId}-{greetingService.Create("ok")}");
    }

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

    private sealed class NullLogger : ILogger
    {
        public void LogError(string message) { }

        public void LogInformation(string message) { }

        public void LogWarning(string message) { }
    }
}
