using Microsoft.Extensions.DependencyInjection;
using System;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public static class NamedServiceRegistryExtensions
    {
        //public static NamedServiceRegistry<TService> PopulateServiceCollection<TService>(this NamedServiceRegistry<TService> registry, IServiceCollection services)
        //{         

        //    foreach (var item in registry.GetRegistrations())
        //    {
        //        if (item.Type != null && item.Instance == null) // todo: and does't have factory func.
        //        {
        //            ServiceLifetime lifetime = (ServiceLifetime)item.Lifetime;
        //            var descriptor = new ServiceDescriptor(item.Type, item.Type, lifetime);
        //            services.Add(descriptor);
        //        }
        //    }

        //    return registry;
        //}

    }
}
