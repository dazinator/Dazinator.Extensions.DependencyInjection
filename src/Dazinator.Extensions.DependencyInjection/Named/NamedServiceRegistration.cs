namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceRegistration<TService> : IDisposable
    {
        // private readonly IServiceProvider _serviceProvider;
        private Action _onDispose = null;

        public NamedServiceRegistration(TService instance, bool registrationOwnsInstance)
        {
            Lifetime = ServiceLifetime.Singleton;
            ImplementationType = null;
            RegistrationOwnsInstance = registrationOwnsInstance;
            InstanceFactory = (sp) => instance;
            if (registrationOwnsInstance)
            {
                _onDispose = () => CheckDispose(instance);
            }
        }

        public NamedServiceRegistration(Func<IServiceProvider> serviceProvider, Type implementationType, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ImplementationType = implementationType;
            if (lifetime == ServiceLifetime.Singleton)
            {
                RegistrationOwnsInstance = true;
                var lazySingleton = new Lazy<TService>(() =>
                {
                    // Perf: we don't worry about calling CreateInstance for singletons - it won't happen often!
                    var sp = serviceProvider();
                    var instance = (TService)ActivatorUtilities.CreateInstance(sp, ImplementationType);
                    if (RegistrationOwnsInstance)
                    {
                        _onDispose = () => CheckDispose(instance);
                    }
                    return instance;
                });

                InstanceFactory = (sp) => lazySingleton.Value; // we don't use current scope sp to resolve singletons, we use root sp, as we don't want singletons to capture scoped references (scoped references can be disposed).
            }
            else if (lifetime == ServiceLifetime.Transient || lifetime == ServiceLifetime.Scoped)
            {
                RegistrationOwnsInstance = false;
                TrackScopedLifetime = (lifetime == ServiceLifetime.Scoped);
                // Perf: we do care about speed of creating transient or scoped instances as it could
                // happen many times during the lifetime of the application. So we use ObjectFactory here.
                var factory = ActivatorUtilities.CreateFactory(ImplementationType, Array.Empty<Type>());
                InstanceFactory = (sp) => (TService)factory(sp, null);
            }

        }

        public NamedServiceRegistration(Func<IServiceProvider> serviceProvider, Func<IServiceProvider, TService> implementationFactory, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ImplementationType = null;
            if (lifetime == ServiceLifetime.Singleton)
            {
                RegistrationOwnsInstance = true;
                var lazySingleton = new Lazy<TService>(() =>
                {
                    var sp = serviceProvider();
                    var instance = implementationFactory(sp);
                    if (RegistrationOwnsInstance)
                    {
                        _onDispose = () => CheckDispose(instance);
                    }
                    return instance;
                });

                InstanceFactory = (sp) => lazySingleton.Value; // we don't use current scope sp to resolve singletons, we use root sp, as we don't want singletons to capture scoped references (scoped references can be disposed).
            }
            else if (lifetime == ServiceLifetime.Transient || lifetime == ServiceLifetime.Scoped)
            {
                TrackScopedLifetime = (lifetime == ServiceLifetime.Scoped);
                RegistrationOwnsInstance = false;
                InstanceFactory = implementationFactory;
            }

        }

        /// <summary>
        /// A registration for a service of type <see cref="TService"/> that simply delegates to the underlying IServiceProvider.
        /// Allows the service to be resolved using normal DI, as well as resolving it as a named service with a default name of <see cref="string.Empty"/> 
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="lifetime"></param>
        public NamedServiceRegistration(Func<IServiceProvider> serviceProvider, ServiceLifetime lifetime)
        {
            Lifetime = lifetime;
            ImplementationType = null;

            // RegistrationOwnsInstance in this context means that either this registration, or the underlying IServiceProvider is responsible for disposing IDisposable instances.
            // In this case as we are dealing with a type or func<t> registration, the underlying IServiceProvider is responsible for all lifetimes of objects.
            // Setting this to true prevent scoped services from having thier lifetime owned by our own ScopedInstanceCache.
            RegistrationOwnsInstance = false; // note IServiceProvider also tracks Transients it creates for disposal.. yuck (not just singletons or scopoed services it creates)

            if (lifetime == ServiceLifetime.Singleton)
            {
                // as per: https://github.com/dazinator/Dazinator.Extensions.DependencyInjection/issues/4
                // delegate to the IServiceProvider to handle lifetime of singleton it creates.             
                var lazySingleton = new Lazy<TService>(() =>
                {
                    // Perf: we don't worry about calling CreateInstance for singletons - it won't happen often!
                    var sp = serviceProvider();
                    var instance = sp.GetRequiredService<TService>();
                    return instance;
                });

                InstanceFactory = (sp) => lazySingleton.Value; // we don't use current scope sp to resolve singletons, we use root sp, as we don't want singletons to capture scoped references (scoped references can be disposed).
            }
            else if (lifetime == ServiceLifetime.Transient || lifetime == ServiceLifetime.Scoped)
            {
                // Perf: we do care about speed of creating transient or scoped instances as it could
                // happen many times during the lifetime of the application. So we use ObjectFactory here.
                InstanceFactory = (sp) => sp.GetRequiredService<TService>();
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

        public Func<IServiceProvider, TService> InstanceFactory { get; }

        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// Whether the instance of the service is going to be disposed via the registration or not.
        /// If True, it means the instance will be disposed when this registration is disposed, which will happen when the underlying registry is disposed - which happens when the underlying container is disposed!
        /// </summary>
        public bool RegistrationOwnsInstance { get; }

        /// <summary>
        /// If True and if this is a Scoped service, then the scoped service will be added to the ScopedInstanceCache
        /// and disposed of when the ScopedInstanceCache is disposed. If the scoped service lifetime is already being
        /// managed by a different mechanism like an underlying IServiceProvider Scope, then this should be false,
        /// as you don't want scoped services to be disposed of twice.
        /// </summary>
        public bool TrackScopedLifetime { get; } = false;

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

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        #endregion

    }
}
