namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceRegistrationFactory<TService>
    {
        private readonly Func<IServiceProvider> _getServiceProvider;

        public NamedServiceRegistrationFactory(Func<IServiceProvider> getServiceProvider) => _getServiceProvider = getServiceProvider;

        /// <summary>
        /// Creates a registration for the service that delegates resolution to the underlying IServiceProvider.
        /// </summary>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create(ServiceLifetime lifetime) => new NamedServiceRegistration<TService>(_getServiceProvider, lifetime);

        /// <summary>
        /// Creates a registration for a service that is created by a factory delegate, with the specified lifetime.
        /// </summary>
        /// <param name="factoryFunc"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create(Func<IServiceProvider, TService> factoryFunc, ServiceLifetime lifetime) => new NamedServiceRegistration<TService>(_getServiceProvider, (sp) => factoryFunc(sp), lifetime);

        /// <summary>
        /// Creates a registration for a service that is provided as a singleton instance.
        /// </summary>
        /// <param name="singletonInstance"></param>
        /// <param name="registrationOwnsLifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create(TService singletonInstance, bool registrationOwnsLifetime) => new NamedServiceRegistration<TService>(singletonInstance, registrationOwnsLifetime);

        /// <summary>
        /// Creates a registration for a service that is specified by an implementation type to be dynamically constructed by the container, and with given the specified lifetime.
        /// </summary>
        /// <param name="implementationType"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create(Type implementationType, ServiceLifetime lifetime)
        {
            NamedServiceRegistration<TService> registration = null;
            registration = new NamedServiceRegistration<TService>(_getServiceProvider, implementationType, lifetime);
            return registration;
        }

    }
}
