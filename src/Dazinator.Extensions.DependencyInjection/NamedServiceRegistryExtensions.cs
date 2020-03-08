using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public static class NamedServiceRegistryExtensions
    {
        public static NamedServiceRegistry<TService> PopulateServiceCollection<TService>(this NamedServiceRegistry<TService> registry, IServiceCollection services)
        {
            services.AddSingleton<NamedServiceRegistry<TService>>((sp) =>
            {
                // tricks the container into thinking it created the object instance,
                // which we need so that it takes ownership of disposing this instance when the container is disposed.
                return registry;
            });

            services.AddScoped<NamedServiceResolver<TService>>();

            var serviceType = typeof(TService);
            services.AddScoped<Func<string, TService>>(sp => new Func<string, TService>(name =>
            {
                var resolver = sp.GetRequiredService<NamedServiceResolver<TService>>();
                return resolver.Get(name);
            }));

            foreach (var item in registry.GetRegistrations())
            {

                if (item.Type != null && item.Instance == null) // todo: and does't have factory func.
                {
                    ServiceLifetime lifetime = (ServiceLifetime)item.Lifetime;
                    var descriptor = new ServiceDescriptor(item.Type, item.Type, lifetime);
                    services.Add(descriptor);
                }
            }

            return registry;
        }

    }
}
