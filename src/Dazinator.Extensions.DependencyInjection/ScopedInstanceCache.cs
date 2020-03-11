namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using System;
    using System.Collections.Concurrent;

    public class ScopedInstanceCache<TService> : IDisposable
    {
        public ScopedInstanceCache() => Instances = new ConcurrentDictionary<string, TService>();
        protected ConcurrentDictionary<string, TService> Instances { get; set; }

        public TService GetOrAdd(string name, Func<TService> instanceFactory) => Instances.GetOrAdd(name, (key) => instanceFactory());

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var values = Instances.Values;
                    foreach (var item in values)
                    {
                        if (item is IDisposable)
                        {
                            ((IDisposable)item)?.Dispose();
                        }
                    }
                }

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
