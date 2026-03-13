using Procedo.Plugin.SDK;

namespace Procedo.UnitTests;

public class PluginRegistryAdvancedTests
{
    [Fact]
    public void TryResolve_Should_Create_New_Instance_Per_Call()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("x", () => new MarkerStep(Guid.NewGuid()));

        registry.TryResolve("x", out var s1);
        registry.TryResolve("x", out var s2);

        Assert.NotSame(s1, s2);
    }

    [Fact]
    public void TryResolve_Should_Propagate_Factory_Exception()
    {
        IPluginRegistry registry = new PluginRegistry();
        registry.Register("x", () => throw new InvalidOperationException("factory boom"));

        Assert.Throws<InvalidOperationException>(() => registry.TryResolve("x", out _));
    }

    [Fact]
    public async Task Register_And_Resolve_Should_Be_ThreadSafe_Under_Load()
    {
        IPluginRegistry registry = new PluginRegistry();

        var registerTasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => registry.Register($"k{i}", () => new MarkerStep(Guid.NewGuid()))));

        await Task.WhenAll(registerTasks);

        var resolveTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() =>
        {
            var key = $"k{i % 100}";
            var ok = registry.TryResolve(key, out var step);
            return ok && step is not null;
        }));

        var results = await Task.WhenAll(resolveTasks);

        Assert.All(results, Assert.True);
    }

    [Fact]
    public async Task TryRegister_And_Resolve_Should_Be_ThreadSafe_Under_Load()
    {
        IPluginRegistry registry = new PluginRegistry();

        var registerTasks = Enumerable.Range(0, 100)
            .Select(i => Task.Run(() => registry.TryRegister($"k{i}", () => new MarkerStep(Guid.NewGuid()))));

        var added = await Task.WhenAll(registerTasks);
        Assert.All(added, Assert.True);

        var resolveTasks = Enumerable.Range(0, 1000).Select(i => Task.Run(() =>
        {
            var key = $"k{i % 100}";
            var ok = registry.TryResolve(key, out var step);
            return ok && step is not null;
        }));

        var results = await Task.WhenAll(resolveTasks);

        Assert.All(results, Assert.True);
    }

    private sealed class MarkerStep(Guid id) : IProcedoStep
    {
        public Task<StepResult> ExecuteAsync(StepContext context)
            => Task.FromResult(new StepResult
            {
                Success = true,
                Outputs = new Dictionary<string, object> { ["id"] = id }
            });
    }
}
