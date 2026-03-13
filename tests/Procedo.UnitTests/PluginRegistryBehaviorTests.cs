using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class PluginRegistryBehaviorTests
{
    [Fact]
    public void TryResolve_Should_Be_Case_Insensitive()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("system.echo", () => new NoopStep());

        var found = registry.TryResolve("SYSTEM.ECHO", out var step);

        Assert.True(found);
        Assert.NotNull(step);
    }

    [Fact]
    public async Task Register_Should_Override_Previous_Registration_For_Same_Key()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("dup.step", () => new TaggedStep("v1"));
        registry.Register("dup.step", () => new TaggedStep("v2"));

        registry.TryResolve("dup.step", out var step);

        var result = await step!.ExecuteAsync(new StepContext());
        Assert.Equal("v2", result.Outputs["tag"]);
    }

    [Fact]
    public async Task TryRegister_Should_Not_Override_Previous_Registration_For_Same_Key()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("dup.step", () => new TaggedStep("v1"));

        var added = registry.TryRegister("dup.step", () => new TaggedStep("v2"));

        Assert.False(added);
        registry.TryResolve("dup.step", out var step);
        var result = await step!.ExecuteAsync(new StepContext());
        Assert.Equal("v1", result.Outputs["tag"]);
    }

    [Fact]
    public void RegisterOrThrow_Should_Throw_For_Duplicate_Key()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("dup.step", () => new TaggedStep("v1"));

        var ex = Assert.Throws<InvalidOperationException>(() => registry.RegisterOrThrow("dup.step", () => new TaggedStep("v2")));
        Assert.Contains("already registered", ex.Message);
    }

    private sealed class NoopStep : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult { Success = true });
    }

    private sealed class TaggedStep(string tag) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["tag"] = tag }
            });
    }
}
