namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    public class NamedServiceResolver<TService> : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Lazy<ScopedInstanceCache<TService>> _scopedInstanceCache;

        public NamedServiceResolver(NamedServiceRegistry<TService> registry, IServiceProvider serviceProvider)
        {
            Registry = registry;
            _serviceProvider = serviceProvider;
            _scopedInstanceCache = new Lazy<ScopedInstanceCache<TService>>();
        }

        public NamedServiceRegistry<TService> Registry { get; }

        public TService this[string name] => Get(name);

        public TService Get(string name)
        {
            // optimised more for transients and singletons - we don't need to cache instances per scope
            // for those, so we only bother to check in scope cache for an instance once we know we dealing with
            // a scoped registration. We coud change this and optimise for scoped registrations, by checking this cache for an instance first,
            // but then that would be a needless lookup (and needlessly initialising the lazy cache instance)
            // in the cases that the registration is singleton or transient.           

            var item = Registry.GetRegistration(name);
            if (item == null)
            {
                // potentially could allow named registration to be added dynamically here:
                // https://github.com/dazinator/Dazinator.Extensions.DependencyInjection/issues/5
                throw new KeyNotFoundException(name);
            }
            // A scoped service flagged with RegistrationOwnsInstance means owner - we don't track it here.
            // Because of that we can just return an instance without adding it to the tracking list (_scopedInstanceCache) 
            if ((item.Lifetime != ServiceLifetime.Scoped) || !item.TrackScopedLifetime)
            {
                var result = item.InstanceFactory(_serviceProvider);
                return result;
            }

            // scoped instances,  of which nothing else is managing their lifetime, we must get or add to scope cache.
            // see https://github.com/dazinator/Dazinator.Extensions.DependencyInjection/issues/4          
            return _scopedInstanceCache.Value.GetOrAdd(name, () => item.InstanceFactory(_serviceProvider));
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
                    if (_scopedInstanceCache.IsValueCreated)
                    {
                        _scopedInstanceCache.Value.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        public void Dispose() => Dispose(true);
#pragma warning restore CA1063 // Implement IDisposable Correctly
        #endregion
    }
}
