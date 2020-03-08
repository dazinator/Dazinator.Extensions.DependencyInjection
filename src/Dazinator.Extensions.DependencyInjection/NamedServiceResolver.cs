using System;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public class NamedServiceResolver<TService>
    {
        private readonly IServiceProvider _serviceProvider;

        public NamedServiceResolver(NamedServiceRegistry<TService> registry, IServiceProvider serviceProvider)
        {
            Registry = registry;
            _serviceProvider = serviceProvider;
        }

        public NamedServiceRegistry<TService> Registry { get; }

        public TService Get(string name)
        {
            var item = Registry.GetRegistration(name);
            if (item == null)
            {
                return default(TService);
            }
            var result = item.GetOrCreateInstance(_serviceProvider);
            return result;
        }
    }
}
