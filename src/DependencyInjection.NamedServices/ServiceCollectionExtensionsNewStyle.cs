namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensionsNewStyle
    {

        /// <summary>
        /// Collate existing named service registrations together to build Named service registries, removing them from IServiceCollection in the process.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection CollateNamed(this IServiceCollection services)
        {

            var namedDescriptorType = typeof(NamedServiceDescriptor);
            var namedRegistrationsGroupedByService = services.Where(s => s.GetType() == namedDescriptorType).GroupBy(s => s.ServiceType).Select(a => a.Key).ToList();

            foreach (var namedServiceType in namedRegistrationsGroupedByService)
            {
                if (namedServiceType == null)
                {
                    continue;
                }

                EnsureNamedRegistryLoaded(namedServiceType, services);
            }

            return services;

        }

        /// <summary>
        /// Collate existing named service registrations together for the specified service type only.
        /// to build Named service registries, removing them from IServiceCollection in the process.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection CollateNamed<TServiceType>(this IServiceCollection services)
        {

            var namedDescriptorType = typeof(NamedServiceDescriptor);
            var namedRegistrationsGroupedByService = services.Where(s => s.GetType() == namedDescriptorType && s.ServiceType == typeof(TServiceType)).Select(a => a.ServiceType).ToList();

            foreach (var namedServiceType in namedRegistrationsGroupedByService)
            {
                if (namedServiceType == null)
                {
                    continue;
                }

                EnsureNamedRegistryLoaded(namedServiceType, services);
            }

            return services;

        }


        private static readonly Type _regGenericType = typeof(NamedServiceRegistry<>);

        private static void EnsureNamedRegistryLoaded(Type serviceType, IServiceCollection services)
        {

            var regConcreteType = _regGenericType.MakeGenericType(serviceType);
            var existing = services.LastOrDefault(s => s.ServiceType == regConcreteType && s.ImplementationInstance != null);
            object registryInstance = null;
            if (existing == null)
            {
                // register named registry for this service type.
                var createInstanceArgs = new object[] { services };
                var instance = Activator.CreateInstance(regConcreteType, createInstanceArgs);

                var extensionsType = typeof(ServiceCollectionExtensions);
                var addServicesMethodInfo = extensionsType.GetMethod(nameof(ServiceCollectionExtensions.AddNamedServicesRegistry));
                if (addServicesMethodInfo == null)
                {
                    throw new InvalidOperationException("AddNamedServicesRegistry extension method not found.");
                }

                var addServicesGenericMethodInfo = addServicesMethodInfo.MakeGenericMethod(serviceType);
                var args = new object[] { services, instance };
                addServicesGenericMethodInfo.Invoke(null, args);
                registryInstance = instance;
            }
            else
            {
                registryInstance = existing.ImplementationInstance;
                var loadMethod = regConcreteType.GetMethod(nameof(NamedServiceRegistry<Type>.LoadFromServices));
                var loadServicesArgs = new object[] { services };
                loadMethod.Invoke(registryInstance, loadServicesArgs);
               // services.Remove(existing);// no longer needed - this was a placeholder.
            }



            //else
            //{
            //    throw new InvalidOperationException("CollateNamed must be called after AddNamed")
            //}


        }

        #region Singleton

        #region Instance

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, string name, TService instance)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), instance);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        ///  Register a named singleton service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="serviceType">The type of service to register</param>
        /// <param name="instance">The implementation instance.</param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton(this IServiceCollection services, string name, Type serviceType, object instance)
        {
            var descriptor = new NamedServiceDescriptor(name, serviceType, instance);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #region Type

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <typeparam name="TService">The type of service to register as well as the implementation type.</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TService), ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register as well as the implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton(this IServiceCollection services, string name, Type type)
        {
            var descriptor = new NamedServiceDescriptor(name, type, type, ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type.</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton<TService, TImplementation>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TImplementation), ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <typeparam name="TService">The service type.</typeparam>
        /// <param name="services"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, string name, Type implementationType)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), implementationType, ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named singleton service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register</param>
        /// <param name="instance">The implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddSingleton(this IServiceCollection services, string name, Type serviceType, Type implementationType)
        {
            var descriptor = new NamedServiceDescriptor(name, serviceType, implementationType, ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #region Factory

        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, string name, Func<IServiceProvider, object> factory)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), factory, ServiceLifetime.Singleton);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #endregion

        #region Scoped

        #region Type

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TService), ServiceLifetime.Scoped);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register and also the implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped(this IServiceCollection services, string name, Type type)
        {
            var descriptor = new NamedServiceDescriptor(name, type, type, ServiceLifetime.Scoped);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation to use for this name</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped<TService, TImplementation>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TImplementation), ServiceLifetime.Scoped);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register</param>
        /// <param name="instance">The implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped(this IServiceCollection services, string name, Type serviceType, Type implementationType)
        {
            var descriptor = new NamedServiceDescriptor(name, serviceType, implementationType, ServiceLifetime.Scoped);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #region Factory

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <typeparam name="TService">The type of service to register</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="factory">Factory method that returns implementation instance.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, string name, Func<IServiceProvider, object> factory)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), factory, ServiceLifetime.Scoped);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #endregion

        #region Transient

        #region Type

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient<TService>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TService), ServiceLifetime.Transient);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register and also the implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient(this IServiceCollection services, string name, Type type)
        {
            var descriptor = new NamedServiceDescriptor(name, type, type, ServiceLifetime.Transient);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <typeparam name="TService">The type of service to register.</typeparam>
        /// <typeparam name="TImplementation">The implementation to use for this name</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient<TService, TImplementation>(this IServiceCollection services, string name)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), typeof(TImplementation), ServiceLifetime.Transient);
            services.Add(descriptor);
            return services;
        }

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The type of service to register</param>
        /// <param name="instance">The implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient(this IServiceCollection services, string name, Type serviceType, Type implementationType)
        {
            var descriptor = new NamedServiceDescriptor(name, serviceType, implementationType, ServiceLifetime.Transient);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #region Factory

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <typeparam name="TService">The type of service to register</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="factory">Factory method that returns implementation instance.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient<TService>(this IServiceCollection services, string name, Func<IServiceProvider, object> factory)
        {
            var descriptor = new NamedServiceDescriptor(name, typeof(TService), factory, ServiceLifetime.Transient);
            services.Add(descriptor);
            return services;
        }

        #endregion

        #endregion
    }

}
