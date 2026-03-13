using Microsoft.Extensions.DependencyInjection;
using Procedo.Engine.Hosting;
using Procedo.Plugin.SDK;

namespace Procedo.Extensions.DependencyInjection;

public sealed class ProcedoServiceBuilder
{
    private readonly List<Action<IServiceProvider, ProcedoHostBuilder>> _configurations = new();

    internal ProcedoServiceBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public ProcedoServiceBuilder ConfigureHost(Action<ProcedoHostBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        _configurations.Add((_, builder) => configure(builder));
        return this;
    }

    public ProcedoServiceBuilder ConfigureHost(Action<IServiceProvider, ProcedoHostBuilder> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        _configurations.Add(configure);
        return this;
    }

    public ProcedoServiceBuilder ConfigurePlugins(Action<IPluginRegistry> configure)
    {
        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        _configurations.Add((_, builder) => builder.ConfigurePlugins(configure));
        return this;
    }

    public ProcedoServiceBuilder RegisterStep(string stepType, Func<StepContext, StepResult> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _configurations.Add((_, builder) => builder.ConfigurePlugins(registry => registry.Register(stepType, handler)));
        return this;
    }

    public ProcedoServiceBuilder RegisterStep(string stepType, Func<StepContext, Task<StepResult>> handler)
    {
        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        _configurations.Add((_, builder) => builder.ConfigurePlugins(registry => registry.Register(stepType, handler)));
        return this;
    }

    public ProcedoServiceBuilder RegisterStep<TStep>(string stepType, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TStep : class, IProcedoStep
    {
        EnsureStepService<TStep>(lifetime);
        _configurations.Add((_, builder) => builder.ConfigurePlugins(registry => registry.Register<TStep>(stepType)));
        return this;
    }

    public ProcedoServiceBuilder RegisterMethod(string stepType, Delegate method)
    {
        if (method is null)
        {
            throw new ArgumentNullException(nameof(method));
        }

        _configurations.Add((_, builder) => builder.ConfigurePlugins(registry => registry.RegisterMethod(stepType, method)));
        return this;
    }

    internal ProcedoHost Build(IServiceProvider serviceProvider)
    {
        var hostBuilder = new ProcedoHostBuilder().UseServiceProvider(serviceProvider);
        foreach (var configure in _configurations)
        {
            configure(serviceProvider, hostBuilder);
        }

        return hostBuilder.Build();
    }

    private void EnsureStepService<TStep>(ServiceLifetime lifetime)
        where TStep : class
    {
        if (Services.Any(static descriptor => descriptor.ServiceType == typeof(TStep)))
        {
            return;
        }

        Services.Add(new ServiceDescriptor(typeof(TStep), typeof(TStep), lifetime));
    }
}
