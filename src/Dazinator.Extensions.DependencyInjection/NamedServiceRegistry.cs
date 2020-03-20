namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Register different 
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class NamedServiceRegistry<TService> : IDisposable
    {
        private readonly IDictionary<string, NamedServiceRegistration<TService>> _namedRegistrations;

        public NamedServiceRegistry()
        {
            //_serviceProvider = serviceProvider;
            _namedRegistrations = new Dictionary<string, NamedServiceRegistration<TService>>();
        }

        public IServiceProvider ServiceProvider { get; set; }

        public NamedServiceRegistration<TService> this[string name] => GetRegistration(name);

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

        public void AddSingleton<TConcreteType>()
where TConcreteType : TService => AddSingleton<TConcreteType>(string.Empty);

        public void AddSingleton(Func<IServiceProvider, TService> factoryFunc) => AddSingleton(string.Empty, factoryFunc);

        public void AddSingleton<TImplementationType>(Func<IServiceProvider, TImplementationType> factoryFunc)
    where TImplementationType : TService => AddSingleton<TImplementationType>(string.Empty, factoryFunc);

        #endregion

        public void AddSingleton(string name)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TService), ServiceLifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        public void AddSingleton<TConcreteType>(string name)
            where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TConcreteType), ServiceLifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        public void AddSingleton(string name, Func<IServiceProvider, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, factoryFunc, ServiceLifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        private IServiceProvider GetServiceProvider()
        {
            if(ServiceProvider == null)
            {
                throw new InvalidOperationException("Service provider is null");
            }
            return ServiceProvider;
        }

        public void AddSingleton<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
          where TImplementationType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, (sp) => factoryFunc(sp), ServiceLifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        /// <summary>
        /// Add an instance to be obtained by name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <param name="registrationOwnsInstance">true means if this instance implements IDisposable, Dispose will be called on this instance when the underlying registration is Disposed. false means you take care of disposal of this instance using your own mechanism, or perhaps its managed by a container already etc.</param>
        public void AddSingleton(string name, TService instance, bool registrationOwnsInstance = false)
        {
            var registration = new NamedServiceRegistration<TService>(instance, registrationOwnsInstance);
            _namedRegistrations.Add(name, registration);
        }
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

        public void AddTransient(string name)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TService), ServiceLifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }

        public void AddTransient<TConcreteType>(string name)
           where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TConcreteType), ServiceLifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }

        public void AddTransient(string name, Func<IServiceProvider, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, factoryFunc, ServiceLifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }

        public void AddTransient<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
           where TImplementationType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, (sp) => factoryFunc(sp), ServiceLifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }
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

        public void AddScoped(string name)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TService), ServiceLifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }

        public void AddScoped<TConcreteType>(string name)
         where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, typeof(TConcreteType), ServiceLifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }

        public void AddScoped(string name, Func<IServiceProvider, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, factoryFunc, ServiceLifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }

        public void AddScoped<TImplementationType>(string name, Func<IServiceProvider, TImplementationType> factoryFunc)
            where TImplementationType : TService
        {
            var registration = new NamedServiceRegistration<TService>(GetServiceProvider, (sp) => factoryFunc(sp), ServiceLifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }
        #endregion
        public NamedServiceRegistration<TService> GetRegistration(string name) => _namedRegistrations[name];

        public IEnumerable<NamedServiceRegistration<TService>> GetRegistrations()
        {
            var values = _namedRegistrations.Values;
            return values;
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
