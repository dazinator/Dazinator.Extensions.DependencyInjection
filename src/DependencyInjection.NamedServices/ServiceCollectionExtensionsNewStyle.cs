namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensionsNewStyle
    {
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
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddSingleton(name, instance);
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
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddSingleton<TService>(name);
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
            where TImplementation : TService
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddSingleton<TImplementation>(name);
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
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddSingleton(name, implementationType);
            return services;
        }

        #endregion

        #region Factory

        public static IServiceCollection AddSingleton<TService>(this IServiceCollection services, string name, Func<IServiceProvider, TService> factory)
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddSingleton(name, factory);
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
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddScoped<TService>(name);
            return services;
        }

        /// <summary>
        /// Register a named scoped service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, string name, Type type)
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddScoped(name, type);
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
            where TImplementation : TService
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddScoped<TImplementation>(name);
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
        public static IServiceCollection AddScoped<TService>(this IServiceCollection services, string name, Func<IServiceProvider, TService> factory)
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddScoped(name, factory);
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
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddTransient(name);
            return services;
        }

        /// <summary>
        /// Register a named transient service.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="type">The implementation type.</param>
        /// <returns></returns>
        public static IServiceCollection AddTransient<TService>(this IServiceCollection services, string name, Type type)
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddTransient(name, type);
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
            where TImplementation : TService
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddTransient<TImplementation>(name);
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
        public static IServiceCollection AddTransient<TService>(this IServiceCollection services, string name, Func<IServiceProvider, TService> factory)
        {
            var reg = ServiceCollectionExtensions.GetOrAddRegistry<TService>(services);
            reg.AddTransient<TService>(name, factory);
            return services;
        }

        #endregion

        #endregion
    }

}
