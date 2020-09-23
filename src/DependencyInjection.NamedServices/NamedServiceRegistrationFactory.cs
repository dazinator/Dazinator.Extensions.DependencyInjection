namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceRegistrationFactory<TService>
    {
        private readonly Func<IServiceProvider> _getServiceProvider;

        public NamedServiceRegistrationFactory(Func<IServiceProvider> getServiceProvider) => _getServiceProvider = getServiceProvider;

        /// <summary>
        /// Creates a registration for the service that simply asks the underlying IServiceProvider to resolve the service. This means you must have registered the service
        /// with the container itself.
        /// </summary>
        /// <param name="lifetime">This is used primarily as a hint to the registration - if its a singleton the registration might keep hold of the instance for future rather than keep requesting the container for it.</param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> CreatePassThrough(ServiceLifetime lifetime) => new NamedServiceRegistration<TService>(_getServiceProvider, lifetime);

        /// <summary>
        /// Creates a registration to activate a service by type, the implementation type is the same type as the service type.
        /// </summary>
        /// <param name="factoryFunc"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create(ServiceLifetime lifetime) => new NamedServiceRegistration<TService>(_getServiceProvider, typeof(TService), lifetime);


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

        /// <summary>
        /// Creates a registration for a service that is specified by an implementation type to be dynamically constructed by the container, and with given the specified lifetime.
        /// </summary>
        /// <param name="implementationType"></param>
        /// <param name="lifetime"></param>
        /// <returns></returns>
        public NamedServiceRegistration<TService> Create<TImplementationType>(ServiceLifetime lifetime)
            where TImplementationType: TService
        {
            NamedServiceRegistration<TService> registration = null;
            registration = new NamedServiceRegistration<TService>(_getServiceProvider, typeof(TImplementationType), lifetime);
            return registration;
        }

    }
}
