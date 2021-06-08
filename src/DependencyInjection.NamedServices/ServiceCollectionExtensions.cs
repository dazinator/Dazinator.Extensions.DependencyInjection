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
            var registry = new NamedServiceRegistry<TService>(services);
            configure?.Invoke(registry);

            //var registry = new NamedServiceRegistry<TService>(()=> {

            //    services.AddSingleton(sp =>
            //    {
            //        registry.
            //        configure(registry);
            //        return registry;
            //    });

            //});

            return AddNamedServicesRegistry(services, registry);
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
