namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceProviderExtensions
    {
        public static TService GetNamed<TService>(this IServiceProvider serviceProvider, string name = "")
        {
            var resolver = serviceProvider.GetRequiredService<NamedServiceResolver<TService>>();
            return resolver.Get(name);
        }

        public static Func<TService> GetNamedFunc<TService>(this IServiceProvider serviceProvider, string name)
        {
            TService func() => serviceProvider.GetNamed<TService>(name);
            return func;
        }
    }

}
