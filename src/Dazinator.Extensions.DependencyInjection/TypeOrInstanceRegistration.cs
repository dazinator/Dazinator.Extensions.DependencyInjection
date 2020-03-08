using System;

namespace Dazinator.Extensions.DependencyInjection
{
    public class TypeOrInstanceRegistration<TService> : IDisposable
    {
        /// <summary>
        /// If it's a type, the type must be registered seperately with DI, as it will be DI that does the underlying lifetime management.
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// If it's an instance, it's assumed to be a singleton as support for transients doesn't yet exist. The lifetime
        /// of this singleton instance is tied to the lifetime of this registration, which means you shouldn't register this with your container,
        /// but this registration will be registered, and disposed when this registration is disposed.
        /// </summary>
        public TService Instance { get; set; }

        public Lifetime Lifetime { get; set; }
        public bool RegistrationOwnsInstance { get; set; }
        public TService GetOrCreateInstance(IServiceProvider serviceProvider)
        {
            if (Instance != null)
            {
                return Instance;
            }
            if(Type != null)
            {
                // if not implementation factory, can just be registered in contauner.
                // Could lock arround initialising the singleton value.
                // and then cache it locally?
                TService result = (TService)serviceProvider.GetService(Type);
                return result;
            }

            throw new InvalidOperationException();
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
                    var disposable = Instance as IDisposable;
                    disposable?.Dispose();
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
