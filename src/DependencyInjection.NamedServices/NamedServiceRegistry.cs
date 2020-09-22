namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.Extensions.DependencyInjection;

    public sealed class NamedServiceRegistry<TService> : IDisposable
    {
        private readonly IDictionary<string, NamedServiceRegistration<TService>> _namedRegistrations;
        private readonly IServiceCollection _services;

        private ReaderWriterLockSlim _namedRegistrationsLock = null;
        private readonly NamedServiceRegistrationFactory<TService> _namedRegistrationFactory;

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
            _namedRegistrationFactory = new NamedServiceRegistrationFactory<TService>(GetServiceProvider);
        }

        public IServiceProvider ServiceProvider { get; set; }

        public NamedServiceRegistration<TService> this[string name] => GetRegistration(name);

        public void UseDynamicLookup(Func<string, NamedServiceRegistrationFactory<TService>, NamedServiceRegistration<TService>> factory)
        {
            if (factory == null)
            {
                DisableDynamicRegistrations();
            }
            else
            {
                EnableDynamicRegistrations(factory);
            }
        }

        public void ForwardName(string from, string to)
        {
            if (ForwardedNameMappings == null)
            {
                ForwardedNameMappings = new Dictionary<string, string>();
            }

            ForwardedNameMappings.Add(from, to);
        }

        private void EnableDynamicRegistrations(Func<string, NamedServiceRegistrationFactory<TService>, NamedServiceRegistration<TService>> factory)
        {
            DynamicLookupRegistration = factory;
            AreDynamicRegistrationsEnabled = true;
            _namedRegistrationsLock = new ReaderWriterLockSlim();
        }

        private void DisableDynamicRegistrations()
        {
            DynamicLookupRegistration = null;
            AreDynamicRegistrationsEnabled = false;
            _namedRegistrationsLock?.Dispose();
            _namedRegistrationsLock = null;
        }

        public Func<string, NamedServiceRegistrationFactory<TService>, NamedServiceRegistration<TService>> DynamicLookupRegistration { get; set; }

        private Dictionary<string, string> ForwardedNameMappings { get; set; }

        private bool AreDynamicRegistrationsEnabled { get; set; }

        public NamedServiceRegistration<TService> GetRegistration(string name)
        {
            if(ForwardedNameMappings!=null)
            {
                if(ForwardedNameMappings.TryGetValue(name, out var newName))
                {
                    name = newName;
                }
            }

            // When dynamic registration lookups are in play it means we need to lock dictionary before every read..
            // therefore this feature is quite annoying, and if the user doesn't use it, then having to check for a lock
            // on reads is a useless waste of resources.
            // so.. we'll check a boolean to determine if the feature is being used, and when it isn't we'll avoid all locking.
            if (!AreDynamicRegistrationsEnabled)
            {
                return _namedRegistrations[name];
            }

            _namedRegistrationsLock.EnterUpgradeableReadLock();
            // _namedRegistrationsLock.EnterReadLock();
            try
            {
                // best case we find it and return without having to upgrade lock.
                if (_namedRegistrations.TryGetValue(name, out var reg))
                {
                    return reg;
                }

                // not found, let's see if we can look it up dynamically..
                var regToAdd = DynamicLookupRegistration?.Invoke(name, _namedRegistrationFactory);
                if (regToAdd == null)
                {
                    // nope..
                    throw new KeyNotFoundException(name);
                }

                // yep, so we now need to add this to our named services cache.
                _namedRegistrationsLock.EnterWriteLock();
                try
                {
                    _namedRegistrations.Add(name, regToAdd);
                    return regToAdd;
                }
                finally
                {
                    _namedRegistrationsLock.ExitWriteLock();
                }

            }
            finally
            {
                _namedRegistrationsLock.ExitUpgradeableReadLock();
            }
        }

        #region Add
        public void Add<TImplementationType>(ServiceLifetime lifetime = ServiceLifetime.Transient, Func<IServiceProvider, TImplementationType> factoryFunc = null)
    where TImplementationType : TService => Add<TImplementationType>(lifetime, string.Empty, factoryFunc);

        public void Add<TImplementationType>(ServiceLifetime lifetime, string name, Func<IServiceProvider, TImplementationType> factoryFunc = null)
            where TImplementationType : TService
        {
            if (factoryFunc == null)
            {
                AddRegistration(name, typeof(TImplementationType), lifetime);
            }
            else
            {
                AddRegistration(name, (sp) => factoryFunc(sp), lifetime);
            }
        }

        public void Add(ServiceLifetime lifetime = ServiceLifetime.Transient, Func<IServiceProvider, TService> factoryFunc = null) => Add(lifetime, string.Empty, factoryFunc);

        public void Add(ServiceLifetime lifetime, string name, Func<IServiceProvider, TService> factoryFunc = null)
        {
            if (factoryFunc == null)
            {
                AddRegistration(name, typeof(TService), lifetime);
            }
            else
            {
                AddRegistration(name, factoryFunc, lifetime);
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
            var reg = _namedRegistrationFactory.Create(singletonInstance, registrationOwnsLifetime);
            if (name != null && name == string.Empty)
            {
                // promote named service with string.Empty name to a normal IServiceCollection registration, whilst also still supporting resolving as a named service with string.Empty.
                AddServiceDescriptor(ServiceLifetime.Singleton, null, null, singletonInstance);
            }

            _namedRegistrations.Add(name, reg);
            return reg;
        }

        private NamedServiceRegistration<TService> AddRegistration(string name, Type implementationType, ServiceLifetime lifetime)
        {
            NamedServiceRegistration<TService> registration = null;

            if (name != null && name == string.Empty)
            {
                // promote named service with string.Empty name to a normal IServiceCollection registration, whilst also still supporting resolving as a named service with string.Empty.
                AddServiceDescriptor(lifetime, implementationType, null);
                registration = _namedRegistrationFactory.Create(lifetime);
            }
            else
            {
                registration = _namedRegistrationFactory.Create(implementationType, lifetime);
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
                registration = _namedRegistrationFactory.Create(lifetime);
            }
            else
            {
                registration = _namedRegistrationFactory.Create((sp) => factoryFunc(sp), lifetime);
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

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!AreDynamicRegistrationsEnabled)
                    {
                        // TODO: dispose managed state (managed objects).
                        foreach (var key in _namedRegistrations.Keys)
                        {
                            var item = _namedRegistrations[key];
                            item.Dispose();
                        }
                    }
                    else
                    {
                        _namedRegistrationsLock.EnterWriteLock();
                        // _namedRegistrationsLock.EnterReadLock();
                        try
                        {
                            foreach (var key in _namedRegistrations.Keys)
                            {
                                var item = _namedRegistrations[key];
                                item.Dispose();
                            }
                        }
                        finally
                        {
                            _namedRegistrationsLock.ExitWriteLock();
                        }
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
