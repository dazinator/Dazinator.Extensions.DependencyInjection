using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.SafeHandles;
using System;
using System.Reflection;

namespace Dazinator.Extensions.DependencyInjection
{
    public class NamedServiceRegistration<TService> : IDisposable
    {
        // private readonly IServiceProvider _serviceProvider;
        private Action _onDispose = null;

        public NamedServiceRegistration(TService instance, bool registrationOwnsInstance)
        {
            Lifetime = Lifetime.Singleton;
            ImplementationType = null;
            RegistrationOwnsInstance = registrationOwnsInstance;
            InstanceFactory = (sp) => instance;
            if (registrationOwnsInstance)
            {
                _onDispose = () => CheckDispose(instance);
            }
        }

        public NamedServiceRegistration(IServiceProvider serviceProvider, Type implementationType, Lifetime lifetime)
        {
            Lifetime = lifetime;
            ImplementationType = implementationType;
            if (lifetime == Lifetime.Singleton)
            {
                RegistrationOwnsInstance = true;
                var lazySingleton = new Lazy<TService>(() =>
                {
                    // Perf: we don't worry about calling CreateInstance for singletons - it won't happen often!
                    var instance = (TService)Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(serviceProvider, ImplementationType);
                    if (RegistrationOwnsInstance)
                    {
                        _onDispose = () => CheckDispose(instance);
                    }
                    return instance;
                });

                InstanceFactory = (sp) => lazySingleton.Value; // we don't use current scope sp to resolve singletons, we use root sp, as we don't want singletons to capture scoped references (scoped references can be disposed).
            }
            else if (lifetime == Lifetime.Transient)
            {
                RegistrationOwnsInstance = false;

                // Perf: we do care about speed of creating transient instances as it could happen a lot.
                // So we use ObjectFactory here.
                var factory = ImplementationType.CreateObjectFactory();
                InstanceFactory = (sp) => (TService)factory(serviceProvider, null); // we don't use current scope sp to resolve singletons, we use root sp, as we don't want singletons to capture scoped references (scoped references can be disposed).
            }

        }

        private void CheckDispose(TService instance)
        {
            if (instance is IDisposable)
            {
                ((IDisposable)instance)?.Dispose();
            }
        }

        public Type ImplementationType { get; }

        protected Func<IServiceProvider, TService> InstanceFactory;

        ///// <summary>
        ///// If it's an instance, it's assumed to be a singleton as support for transients doesn't yet exist. The lifetime
        ///// of this singleton instance is tied to the lifetime of this registration, which means you shouldn't register this with your container,
        ///// but this registration will be registered, and disposed when this registration is disposed.
        ///// </summary>
        //public TService Instance { get; set; }

        public Lifetime Lifetime { get; }
        public bool RegistrationOwnsInstance { get; }
        public TService GetOrCreateInstance(IServiceProvider serviceProvider = null)
        {
            var instance = InstanceFactory(serviceProvider); // when resolving singletones, we disregard current scope (serviceProvider) and resolve from root scope.
            return instance;
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
                    _onDispose?.Invoke();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TypeOrInstanceRegistration()
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


    public enum Lifetime
    {
        Singleton = 0,
        Scoped = 1,
        Transient = 2
    }




}
