using System;
using System.Collections.Generic;

namespace Dazinator.Extensions.DependencyInjection
{

    /// <summary>
    /// Register different 
    /// </summary>
    /// <typeparam name="TService"></typeparam>
    public class NamedServiceRegistry<TService> : IDisposable
    {
        private IDictionary<string, TypeOrInstanceRegistration<TService>> _namedRegistrations;

        public NamedServiceRegistry()
        {
            _namedRegistrations = new Dictionary<string, TypeOrInstanceRegistration<TService>>();
        }

        public void AddSingleton(string name)
        {
            var registration = new TypeOrInstanceRegistration<TService>() { Type = typeof(TService), Lifetime = Lifetime.Singleton };
            _namedRegistrations.Add(name, registration);
        }

        public void AddSingleton<TConcreteType>(string name)
            where TConcreteType : TService
        {
            var registration = new TypeOrInstanceRegistration<TService>() { Type = typeof(TConcreteType), Lifetime = Lifetime.Singleton };
            _namedRegistrations.Add(name, registration);
        }

        /// <summary>
        /// Add an instance to be obtained by name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="instance"></param>
        /// <param name="registrationOwnsInstance">true means if this instance implements IDisposable, Dispose will be called on this instance when the underlying registration is Disposed. false means you take care of disposal of this instance using your own mechanism, or perhaps its managed by a container already etc.</param>
        public void AddSingleton(string name, TService instance, bool registrationOwnsInstance = true)
        {
            var registration = new TypeOrInstanceRegistration<TService>() { Instance = instance, RegistrationOwnsInstance = registrationOwnsInstance, Lifetime = Lifetime.Singleton };
            _namedRegistrations.Add(name, registration);
        }

        public TypeOrInstanceRegistration<TService> GetRegistration(string name)
        {
            return _namedRegistrations[name];
        }


        public IEnumerable<TypeOrInstanceRegistration<TService>> GetRegistrations()
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

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~NamedServiceRegistry()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion



    }


}
