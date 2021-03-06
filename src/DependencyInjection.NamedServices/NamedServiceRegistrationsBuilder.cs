namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A utility class that can be used to constrain named service registrations to a particular service type
    /// of <typeparamref name="TService"/>.
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class NamedServiceRegistrationsBuilder<TService>
    {

        public NamedServiceRegistrationsBuilder(IServiceCollection services) => Services = services;

        #region Singleton

        #region Instance


        public IServiceCollection Services { get; set; }

        /// <summary>
        ///  Register a singleton TService.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <param name="instance">The implementation instance.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddSingleton(string name, TService instance)
        {
            Services.AddSingleton<TService>(name, instance);
            return this;
        }

        #endregion

        #region Type

        /// <summary>
        ///  Register a singleton TService.
        /// </summary>
        /// <typeparam name="TImplementationType"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddSingleton<TImplementationType>(string name)
            where TImplementationType : TService
        {
            Services.AddSingleton<TService, TImplementationType>(name);
            return this;
        }

        /// <summary>
        ///  Register a singleton TService.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <param name="type">The implementation type of service to register.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddSingleton(string name, Type implementationType)
        {
            // todo: use generic overload once dependnecy updated.
            Services.AddSingleton<TService>(name, implementationType);
            return this;
        }


        #endregion

        #region Factory

        /// <summary>
        ///  Register a singleton TService.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <param name="factory"></param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddSingleton(string name, Func<IServiceProvider, TService> factory)
        {
            Services.AddSingleton<TService>(name, (sp) => factory(sp));
            return this;
        }


        #endregion

        #endregion

        #region Scoped

        #region Type

        /// <summary>
        ///  Register a scoped TService.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddScoped<TImplementationType>(string name)
            where TImplementationType : TService
        {
            Services.AddScoped<TService, TImplementationType>(name);
            return this;
        }

        /// <summary>
        ///  Register a scoped TService.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <param name="type">The implementation type.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddScoped(string name, Type implementationType)
        {
            Services.AddScoped<TService>(name, implementationType);
            return this;

        }

        #endregion

        #region Factory

        /// <summary>
        ///  Register a scoped TService.
        /// </summary>
        /// <typeparam name="TService">The type of service to register</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="factory">Factory method that returns implementation instance.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddScoped(string name, Func<IServiceProvider, TService> factory)
        {
            Services.AddScoped<TService>(name, (sp) => factory(sp));
            return this;
        }

        #endregion

        #endregion

        #region Transient

        #region Type

        /// <summary>
        ///  Register a transient TService.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddTransient<TImplementationType>(string name)
            where TImplementationType : TService
        {
            Services.AddTransient<TService, TImplementationType>(name);
            return this;

        }

        /// <summary>
        ///  Register a transient TService.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="name">The name for the job type.</param>
        /// <param name="type">The implementation type.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddTransient(string name, Type implementationType)
        {
            Services.AddTransient<TService>(name, implementationType);
            return this;
        }

        #endregion

        #region Factory

        /// <summary>
        ///  Register a transient TService.
        /// </summary>
        /// <typeparam name="TService">The type of service to register</typeparam>
        /// <param name="services"></param>
        /// <param name="name">The name for the named service registration.</param>
        /// <param name="factory">Factory method that returns implementation instance.</param>
        /// <returns></returns>
        public NamedServiceRegistrationsBuilder<TService> AddTransient(string name, Func<IServiceProvider, TService> factory)
        {
            Services.AddTransient<TService>(name, (sp) => factory(sp));
            return this;
        }

        #endregion

        #endregion      

    }

}
