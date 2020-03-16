namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using System;
    using System.Collections.Generic;
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
            // for those, so we only bother to check in scope cache for an instance once we know we are not dealing with
            // a scoped registration. We coud change this and optimise for scoped registrations, by checking this cache for an instance first,
            // but then that would be a needless lookup (and needlessly initialising the lazy cache instance)
            // in the cases that the registration is singleton or transient.
            var item = Registry.GetRegistration(name);
            if (item == null)
            {
                throw new KeyNotFoundException(name);
            }
            if (item.Lifetime != Lifetime.Scoped)
            {
                var result = item.InstanceFactory(_serviceProvider);
                return result;
            }

            // scoped instances, we must get or add to scope cache.
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
