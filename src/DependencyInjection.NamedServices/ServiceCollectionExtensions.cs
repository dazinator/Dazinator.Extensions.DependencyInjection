namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;

    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNamed<TService>(this IServiceCollection services, Action<NamedServiceRegistry<TService>> configure = null)
        {
            // look for existing registry
            var registry = GetOrAddRegistry<TService>(services);
            configure?.Invoke(registry);
            return services;
        }

        internal static NamedServiceRegistry<TService> GetOrAddRegistry<TService>(IServiceCollection services)
        {
            var regType = typeof(NamedServiceRegistry<TService>);
            var existing = services.LastOrDefault(s => s.ServiceType == regType && s.ImplementationInstance != null);
            if (existing == null)
            {
                var reg = new NamedServiceRegistry<TService>(services);
                AddNamedServicesRegistry<TService>(services, reg);
                return reg;
            }
            else
            {
                var registryInstance = existing.ImplementationInstance as NamedServiceRegistry<TService>;
                return registryInstance;
            }

        }


        public static IServiceCollection AddNamedServicesRegistry<TService>(IServiceCollection services, NamedServiceRegistry<TService> registry)
        {
            services.AddSingleton(registry); // this is added so we can discover the instance later from this registration. The registration isn't used for actual DI is its overidden immediately below.
            services.AddSingleton(sp =>
            {
                registry.ServiceProvider = sp;
                return registry;
            });

            services.AddScoped<NamedServiceResolver<TService>>();

            var serviceType = typeof(TService);
            services.AddScoped(sp => new Func<string, TService>(name =>
            {
                var resolver = sp.GetRequiredService<NamedServiceResolver<TService>>();
                return resolver.Get(name);
            }));

            return services;
        }

    }

}
