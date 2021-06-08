namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceDescriptor : ServiceDescriptor
    {
        public NamedServiceDescriptor(string name, Type serviceType, object instance) : base(serviceType, instance)
        {
            Name = name;
        }

        public NamedServiceDescriptor(string name, Type serviceType, Type implementationType, ServiceLifetime lifetime) : base(serviceType, implementationType, lifetime)
        {
            Name = name;
        }

        public NamedServiceDescriptor(string name, Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime) : base(serviceType, factory, lifetime)
        {
            Name = name;
        }

        public string Name { get; private set; }

    }

}
