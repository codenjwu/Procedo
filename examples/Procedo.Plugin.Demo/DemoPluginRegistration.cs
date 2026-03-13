using System.Collections.Concurrent;
using Procedo.Plugin.SDK;

namespace Procedo.Plugin.Demo;

public static class DemoPluginRegistration
{
    private static readonly ConcurrentDictionary<string, int> InvocationCounts = new(StringComparer.OrdinalIgnoreCase);

    public static IPluginRegistry AddDemoPlugin(this IPluginRegistry registry)
    {
        registry.Register("demo.flaky", () => new FlakyStep(InvocationCounts));
        registry.Register("demo.sleep", () => new SleepStep());
        registry.Register("demo.fail", () => new FailStep());
        registry.Register("demo.fail_once", () => new FailOnceStep(InvocationCounts));
        registry.Register("demo.cancel", () => new CancelStep());
        registry.Register("demo.quality", () => new QualityStep());
        registry.Register("demo.score", () => new ScoreStep());
        return registry;
    }
}
