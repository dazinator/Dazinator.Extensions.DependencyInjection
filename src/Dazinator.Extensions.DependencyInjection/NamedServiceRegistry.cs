namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceRegistry<TService> : IDisposable
    {
        private readonly IDictionary<string, NamedServiceRegistration<TService>> _namedRegistrations;
        private readonly IServiceCollection _services;

        /// <summary>
        /// Construct the registry providing an <see cref="IServiceCollection"/> in which case any services registered with an empty name will also be promoted into the IServiceCollection so that you can register them directly with the IServiceProvider as essentially they are "nameless" registrations that can be resolved in the normal mannor. The benefit of this
        /// is that you can keep all your registrations for this service (even the nameless registration) in one place.
        /// This is an optional convenience.
        /// </summary>
        /// <param name="services"></param>
        public NamedServiceRegistry(IServiceCollection services = null)
        {
            _namedRegistrations = new Dictionary<string, NamedServiceRegistration<TService>>();
            _services = services;
        }

        public IServiceProvider ServiceProvider { get; set; }

        public NamedServiceRegistration<TService> this[string name] => GetRegistration(name);

        public NamedServiceRegistration<TService> GetRegistration(string name) => _namedRegistrations[name];

        public IEnumerable<NamedServiceRegistration<TService>> GetRegistrations()
        {
            var values = _namedRegistrations.Values;
            return values;
        }

        #region Add
        public void Add<TImplementationType>(ServiceLifetime lifetime = ServiceLifetime.Transient, Func<IServiceProvider, TImplementationType> factoryFunc = null)
    where TImplementationType : TService => Add<TImplementationType>(lifetime, string.Empty, factoryFunc);

        public void Add<TImplementationType>(ServiceLifetime lifetime, string name, Func<IServiceProvider, TImplementationType> factoryFunc = null)
            where TImplementationType : TService
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    if (factoryFunc == null)
                    {
                        AddScoped<TImplementationType>(name);
                    }
                    else
                    {
                        AddScoped(name, factoryFunc);
                    }
                    break;
                case ServiceLifetime.Singleton:
                    if (factoryFunc == null)
                    {
                        AddSingleton<TImplementationType>(name);
                    }
                    else
                    {
                        AddSingleton(name, factoryFunc);
                    }
                    break;
                case ServiceLifetime.Transient:
                    if (factoryFunc == null)
                    {
                        AddTransient<TImplementationType>(name);
                    }
                    else
                    {
                        AddTransient(name, factoryFunc);
                    }
                    break;
            }
        }

        public void Add(ServiceLifetime lifetime = ServiceLifetime.Transient, Func<IServiceProvider, TService> factoryFunc = null) => Add(lifetime, string.Empty, factoryFunc);

        public void Add(ServiceLifetime lifetime, string name, Func<IServiceProvider, TService> factoryFunc = null)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    if (factoryFunc == null)
                    {
                        AddScoped(name);
                    }
                    else
                    {
                        AddScoped(name, factoryFunc);
                    }
                    break;
                case ServiceLifetime.Singleton:
                    if (factoryFunc == null)
                    {
                        AddSingleton(name);
                    }
                    else
                    {
                        AddSingleton(name, factoryFunc);
                    }
                    break;
                case ServiceLifetime.Transient:
                    if (factoryFunc == null)
                    {
                        AddTransient(name);
                    }
                    else
                    {
                        AddTransient(name, factoryFunc);
                    }
                    break;
            }
        }
        #endregion

        #region Singleton

        #region DefaultName

        public void AddSingleton() => AddSingleton(string.Empty);

        /// <summary>
        /// Add a default instance to be obtained by name of String.Empty OR directly injected as TService
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <param name="registrationOwnsInstance">true means if this instance implements IDisposable, Dispose will be called on this instance when the underlying registration is Disposed. false means you take care of disposal of this instance using your own mechanism, or perhaps its managed by a container already etc.</param>
        public void AddSingleton(TService instance, bool registrationOwnsInstance = false) => AddSingleton(string.Empty, instance, registrationOwnsInstance);

        public void AddSingleton<TConcreteType>()
            where TConcreteType : TService => AddSingleton<TConcreteType>(string.Empty);

        public void AddSingleton(Func<IServiceProvider, TService> factoryFunc) => AddSingleton(string.Empty, factoryFunc);

        public void AddSingleton<TImplementationType>(Func<IServiceProvider, TImplementationType> factoryFunc)
            where TImplementationType : TService => AddSingleton<TImplementationType>(string.Empty, factoryFunc);

        #endregion

        public void AddSingleton(string name) => AddSingleton<TService>(name);

        public void AddSingleton<TConcreteType>(string name)
            where TConcreteType : TService => _ = AddRegistration(name, typeof(TConcreteType), ServiceLifetime.Singleton);

        public void AddSingleton(string name, Func<IServiceProvider, TService> factoryFunc) => _ = AddRegistration(name, factoryFunc, ServiceLifetime.Singleton);

        private IServiceProvider GetServiceProvider()
        {
            if (ServiceProvider == null)
            {
                throw new InvalidOperationException("Service provider is null");
            }
            return ServiceProvider;
        }

        public void AddSingleton<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
          where TImplementationType : TService => _ = AddRegistration(name, (sp) => factoryFunc(sp), ServiceLifetime.Singleton);

        /// <summary>
        /// Add an instance to be obtained by name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <param name="registrationOwnsInstance">true means if this instance implements IDisposable, Dispose will be called on this instance when the underlying registration is Disposed. false means you take care of disposal of this instance using your own mechanism, or perhaps its managed by a container already etc.</param>
        public void AddSingleton(string name, TService instance, bool registrationOwnsInstance = false) => _ = AddRegistration(name, instance, registrationOwnsInstance);

        #endregion

        #region Transient

        #region DefaultName

        public void AddTransient() => AddTransient(string.Empty);

        public void AddTransient<TConcreteType>()
     where TConcreteType : TService => AddTransient<TConcreteType>(string.Empty);

        public void AddTransient(Func<IServiceProvider, TService> factoryFunc) => AddTransient(string.Empty, factoryFunc);

        public void AddTransient<TImplementationType>(Func<IServiceProvider, TImplementationType> factoryFunc)
    where TImplementationType : TService => AddTransient<TImplementationType>(string.Empty, factoryFunc);

        #endregion

        public void AddTransient(string name) => _ = AddRegistration(name, typeof(TService), ServiceLifetime.Transient);

        public void AddTransient<TConcreteType>(string name)
           where TConcreteType : TService => _ = AddRegistration(name, typeof(TConcreteType), ServiceLifetime.Transient);

        public void AddTransient(string name, Func<IServiceProvider, TService> factoryFunc) => _ = AddRegistration(name, factoryFunc, ServiceLifetime.Transient);

        public void AddTransient<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
           where TImplementationType : TService => _ = AddRegistration(name, (sp) => factoryFunc(sp), ServiceLifetime.Transient);
        #endregion

        #region Scoped

        #region DefaultName

        public void AddScoped() => AddScoped(string.Empty);

        public void AddScoped<TConcreteType>()
    where TConcreteType : TService => AddScoped<TConcreteType>(string.Empty);

        public void AddScoped(Func<IServiceProvider, TService> factoryFunc) => AddScoped(string.Empty, factoryFunc);

        public void AddScoped<TImplementationType>(Func<IServiceProvider, TImplementationType> factoryFunc)
    where TImplementationType : TService => AddScoped<TImplementationType>(string.Empty, factoryFunc);

        #endregion

        public void AddScoped(string name) => _ = AddRegistration(name, typeof(TService), ServiceLifetime.Scoped);

        public void AddScoped<TConcreteType>(string name)
         where TConcreteType : TService => _ = AddRegistration(name, typeof(TConcreteType), ServiceLifetime.Scoped);

        public void AddScoped(string name, Func<IServiceProvider, TService> factoryFunc) => _ = AddRegistration(name, factoryFunc, ServiceLifetime.Scoped);

        public void AddScoped<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
            where TImplementationType : TService => _ = AddRegistration(name, (sp) => factoryFunc(sp), ServiceLifetime.Scoped);

        #endregion

        private NamedServiceRegistration<TService> AddRegistration(string name, TService singletonInstance, bool registrationOwnsLifetime)
        {
            NamedServiceRegistration<TService> registration = null;

            if (name != null && name == string.Empty)
            {
                // promote named service with string.Empty name to a normal IServiceCollection registration, whilst also still supporting resolving as a named service with string.Empty.
                AddServiceDescriptor(ServiceLifetime.Singleton, null, null, singletonInstance);
                registration = new NamedServiceRegistration<TService>(singletonInstance, registrationOwnsLifetime);
            }
            else
            {
                registration = new NamedServiceRegistration<TService>(singletonInstance, registrationOwnsLifetime);
            }
            _namedRegistrations.Add(name, registration);
            return registration;
        }

        private NamedServiceRegistration<TService> AddRegistration(string name, Type implementationType, ServiceLifetime lifetime)
        {
            NamedServiceRegistration<TService> registration = null;

            if (name != null && name == string.Empty)
            {
                // promote named service with string.Empty name to a normal IServiceCollection registration, whilst also still supporting resolving as a named service with string.Empty.
                AddServiceDescriptor(lifetime, implementationType, null);
                registration = new NamedServiceRegistration<TService>(GetServiceProvider, lifetime);
            }
            else
            {
                registration = new NamedServiceRegistration<TService>(GetServiceProvider, implementationType, lifetime);
            }
            _namedRegistrations.Add(name, registration);
            return registration;
        }

        private NamedServiceRegistration<TService> AddRegistration(string name, Func<IServiceProvider, TService> factoryFunc, ServiceLifetime lifetime)
        {
            NamedServiceRegistration<TService> registration = null;

            if (name != null && name == string.Empty)
            {
                // promote named service with string.Empty name to a normal IServiceCollection registration, whilst also still supporting resolving as a named service with string.Empty.
                AddServiceDescriptor(lifetime, null, (sp) => factoryFunc(sp));
                registration = new NamedServiceRegistration<TService>(GetServiceProvider, lifetime);
            }
            else
            {
                registration = new NamedServiceRegistration<TService>(GetServiceProvider, (sp) => factoryFunc(sp), lifetime);
            }
            _namedRegistrations.Add(name, registration);
            return registration;
        }


        private void AddServiceDescriptor(ServiceLifetime lifetime, Type implementationType, Func<IServiceProvider, object> implementationFactory, object singletonInstance = null)
        {
            if (_services != null)
            {
                ServiceDescriptor descriptor = null;
                if (singletonInstance != null)
                {
                    descriptor = new ServiceDescriptor(typeof(TService), singletonInstance); // instance is not owned by the ServiceCollection.
                                                                                             // it might be owned by the NamedServiceRegistration is ownership flag set to true against that registration - in which case instance will be disposed when registration is disposed - which won't happen until the service provider is disposed - so should all be good. That flag is dangerous which is why it defaults to false - you have to opt-in to letting the registration own that instance lifetime.
                }

                // IDisposable services registered by type or Func<Type>
                // will be "owned" by the service provider when registered in the service collection. This means
                //1. It will track transients for disposal (yuck) (whether registered by Type or Func<Type>)
                //2. It will track scoped for disposal (whether registered by Type or Func<Type>) OK
                //4. It will track singletons for disposal (only if registered by Type or Func<Type> OK

                else if (implementationType != null)
                {
                    // In all instances, we want our 
                    descriptor = new ServiceDescriptor(typeof(TService), implementationType, lifetime);
                }
                else
                {
                    descriptor = new ServiceDescriptor(typeof(TService), implementationFactory, lifetime);
                }

                _services.Add(descriptor);

            }

        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    foreach (var key in _namedRegistrations.Keys)
                    {
                        var item = _namedRegistrations[key];
                        item.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose() => Dispose(true);
#pragma warning restore CA1063 // Implement IDisposable Correctly
        #endregion



    }


}
