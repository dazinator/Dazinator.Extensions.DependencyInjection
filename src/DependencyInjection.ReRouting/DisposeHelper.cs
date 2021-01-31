namespace DependencyInjection.ReRouting
{
    using System;
    using System.Threading.Tasks;

    public static class DisposeHelper
    {

#if SUPPORTS_ASYNC_DISPOSE
        public static async Task DisposeAsyncIfImplemented(object objectToBeDisposed)
        {
            if (objectToBeDisposed is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                DisposeIfImplemented(objectToBeDisposed);
            }
        }

#endif

        public static void DisposeIfImplemented(object objectToBeDisposed)
        {
            if (objectToBeDisposed is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
