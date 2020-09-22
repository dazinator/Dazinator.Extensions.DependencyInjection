namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static partial class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNamed<TService>(this IServiceCollection services, Action<NamedServiceRegistry<TService>> configure)
        {
            var registry = new NamedServiceRegistry<TService>(services);
            configure(registry);

            //var registry = new NamedServiceRegistry<TService>(()=> {

            //    services.AddSingleton(sp =>
            //    {
            //        registry.
            //        configure(registry);
            //        return registry;
            //    });

            //});

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