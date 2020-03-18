namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Register different 
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class NamedServiceRegistry<TService> : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDictionary<string, NamedServiceRegistration<TService>> _namedRegistrations;

        public NamedServiceRegistry(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _namedRegistrations = new Dictionary<string, NamedServiceRegistration<TService>>();
        }

        public NamedServiceRegistration<TService> this[string name] => GetRegistration(name);

        #region Singleton
        public void AddSingleton(string name)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TService), Lifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        public void AddSingleton<TConcreteType>(string name)
            where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TConcreteType), Lifetime.Singleton);
            _namedRegistrations.Add(name, registration);
        }

        public void AddSingleton(string name, Func<IServiceProvider, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, factoryFunc, Lifetime.Singleton);
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
        public void AddTransient(string name)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TService), Lifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }

        public void AddTransient<TConcreteType>(string name)
           where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TConcreteType), Lifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }

        public void AddTransient(string name, Func<object, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, factoryFunc, Lifetime.Transient);
            _namedRegistrations.Add(name, registration);
        }
        #endregion

        #region Scoped

        public void AddScoped(string name)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TService), Lifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }

        public void AddScoped<TConcreteType>(string name)
         where TConcreteType : TService
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, typeof(TConcreteType), Lifetime.Scoped);
            _namedRegistrations.Add(name, registration);
        }

        public void AddScoped(string name, Func<object, TService> factoryFunc)
        {
            var registration = new NamedServiceRegistration<TService>(_serviceProvider, factoryFunc, Lifetime.Scoped);
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
