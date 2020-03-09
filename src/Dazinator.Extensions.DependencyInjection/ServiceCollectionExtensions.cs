using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNamed<TService>(this IServiceCollection services, Action<NamedServiceRegistry<TService>> configure)
        {

            services.AddSingleton<NamedServiceRegistry<TService>>(sp => {
                var registry = new NamedServiceRegistry<TService>(sp);
                configure(registry);
                return registry;
            });

            services.AddScoped<NamedServiceResolver<TService>>();

            var serviceType = typeof(TService);
            services.AddScoped<Func<string, TService>>(sp => new Func<string, TService>(name =>
            {
                var resolver = sp.GetRequiredService<NamedServiceResolver<TService>>();
                return resolver.Get(name);
            }));

            // Note: for the trick to work, 
            // NamedServiceRegistry<TService> must be resolved through the container atleast once,
            // otherwise it will think it hasn't created an instance, and so it won't call Dispose on it if you dispose the container.
            return services;
        }
    }
}
