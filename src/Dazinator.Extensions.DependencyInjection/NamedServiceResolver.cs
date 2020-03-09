namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using System;
    using System.Collections.Generic;

    public class NamedServiceResolver<TService>
    {
        private readonly IServiceProvider _serviceProvider;

        public NamedServiceResolver(NamedServiceRegistry<TService> registry, IServiceProvider serviceProvider)
        {
            Registry = registry;
            _serviceProvider = serviceProvider;
        }

        public NamedServiceRegistry<TService> Registry { get; }

        public TService this[string name] => Get(name);

        public TService Get(string name)
        {
            var item = Registry.GetRegistration(name);
            if (item == null)
            {
                throw new KeyNotFoundException(name);
            }
            var result = item.GetOrCreateInstance(_serviceProvider);
            return result;
        }
    }
}
