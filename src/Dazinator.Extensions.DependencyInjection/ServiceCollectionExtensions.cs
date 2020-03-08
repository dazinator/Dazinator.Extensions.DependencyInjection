using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNamed<TService>(this IServiceCollection services, Action<NamedServiceRegistry<TService>> configure)
        {
            var registry = new NamedServiceRegistry<TService>();
            configure(registry);
            registry.PopulateServiceCollection(services);

            services.AddSingleton<NamedServiceRegistry<TService>>(sp => {
                // trick the di container to think it created this singleton so that it takes
                // responsibility for disposing it if the container is disposed.
                return registry;
            });

            // Note: for the trick to work, 
            // NamedServiceRegistry<TService> must be resolved through the container atleast once,
            // otherwise it will think it hasn't created an instance, and so it won't call Dispose on it if you dispose the container.
            return services;
        }
    }
}
