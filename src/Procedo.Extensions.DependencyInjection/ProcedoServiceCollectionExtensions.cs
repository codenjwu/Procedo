using Microsoft.Extensions.DependencyInjection;
using Procedo.Engine.Hosting;

namespace Procedo.Extensions.DependencyInjection;

public static class ProcedoServiceCollectionExtensions
{
    public static ProcedoServiceBuilder AddProcedo(this IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        var existingBuilder = TryGetBuilder(services);
        if (existingBuilder is not null)
        {
            return existingBuilder;
        }

        var builder = new ProcedoServiceBuilder(services);
        services.AddSingleton(builder);

        if (!services.Any(static descriptor => descriptor.ServiceType == typeof(ProcedoHost)))
        {
            services.AddSingleton(static serviceProvider =>
            {
                var procedoBuilder = serviceProvider.GetRequiredService<ProcedoServiceBuilder>();
                return procedoBuilder.Build(serviceProvider);
            });
        }

        return builder;
    }

    private static ProcedoServiceBuilder? TryGetBuilder(IServiceCollection services)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ProcedoServiceBuilder) && descriptor.ImplementationInstance is ProcedoServiceBuilder builder)
            {
                return builder;
            }
        }

        return null;
    }
}
