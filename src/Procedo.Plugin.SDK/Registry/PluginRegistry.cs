using System;
using System.Collections.Concurrent;

namespace Procedo.Plugin.SDK;

public sealed class PluginRegistry : IPluginRegistry
{
    private readonly ConcurrentDictionary<string, Func<IProcedoStep>> _factories =
        new(StringComparer.OrdinalIgnoreCase);

    public IServiceProvider? ServiceProvider { get; set; }

    public void Register(string stepType, Func<IProcedoStep> factory)
    {
        Validate(stepType, factory);
        _factories[stepType] = factory;
    }

    public bool TryRegister(string stepType, Func<IProcedoStep> factory)
    {
        Validate(stepType, factory);
        return _factories.TryAdd(stepType, factory);
    }

    public void RegisterOrThrow(string stepType, Func<IProcedoStep> factory)
    {
        Validate(stepType, factory);
        if (!_factories.TryAdd(stepType, factory))
        {
            throw new InvalidOperationException($"A step type '{stepType}' is already registered.");
        }
    }

    public bool Contains(string stepType)
    {
        if (string.IsNullOrWhiteSpace(stepType))
        {
            return false;
        }

        return _factories.ContainsKey(stepType);
    }

    public bool TryResolve(string stepType, out IProcedoStep? step)
    {
        step = null;

        if (!_factories.TryGetValue(stepType, out var factory))
        {
            return false;
        }

        step = factory();
        return true;
    }

    private static void Validate(string stepType, Func<IProcedoStep> factory)
    {
        if (string.IsNullOrWhiteSpace(stepType))
        {
            throw new ArgumentException("Step type is required.", nameof(stepType));
        }

        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }
    }
}
