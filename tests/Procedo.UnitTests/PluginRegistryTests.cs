using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class PluginRegistryTests
{
    [Fact]
    public void Register_And_Resolve_Should_Return_Plugin_Instance()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("demo.test", () => new TestStep());

        var resolved = registry.TryResolve("demo.test", out var step);

        Assert.True(resolved);
        Assert.NotNull(step);
    }

    [Fact]
    public void Contains_Should_Return_True_For_Registered_Step()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("demo.test", () => new TestStep());

        Assert.True(registry.Contains("demo.test"));
        Assert.True(registry.Contains("DEMO.TEST"));
    }

    private sealed class TestStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }
}
