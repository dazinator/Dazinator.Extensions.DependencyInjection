namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Decorates an inner service provider, adapting it to implement <see cref="IDisposable.Dispose"/>
    /// and <see cref="IAsyncDisposable.DisposeAsync"/> - so that additional provided callbacks can be fired on disposal.
    /// </summary>
    public class DisposableServiceProvider : IServiceProvider, IDisposable
#if SUPPORTS_ASYNC_DISPOSE
        , IAsyncDisposable
#endif
    {
        private IServiceProvider _inner;
        private Action _onDispose;

#if SUPPORTS_ASYNC_DISPOSE
        private Func<Task> _onAsyncDispose;
#endif

        public DisposableServiceProvider(IServiceProvider inner,
            Action onDispose = null
#if SUPPORTS_ASYNC_DISPOSE
        , Func<Task> onAsyncDispose = null
#endif
        )
        {
            _inner = inner;
            _onDispose = onDispose;
#if SUPPORTS_ASYNC_DISPOSE
            _onAsyncDispose = onAsyncDispose;
#endif
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public object GetService(Type serviceType) => _inner.GetService(serviceType);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose of inner provider first.
                if (_inner is IDisposable innerDisposable)
                {
                    innerDisposable.Dispose();
                }

                _onDispose?.Invoke();
            }
            _inner = null;
            _onDispose = null;
#if SUPPORTS_ASYNC_DISPOSE
            _onAsyncDispose = null;
#endif
        }

#if SUPPORTS_ASYNC_DISPOSE
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {

            if (_inner is IAsyncDisposable innerDisposableAsync)
            {
                await innerDisposableAsync.DisposeAsync();
            }
            else if (_inner is IDisposable innerDisposable)
            {
                innerDisposable.Dispose();
            }

            if (_onAsyncDispose != null)
            {
                await _onAsyncDispose().ConfigureAwait(false);
            }

            //if (_onDispose != null)
            //{
            //    _onDispose?.Invoke();
            //}

            _onAsyncDispose = null;
            _onDispose = null;
            _inner = null;
        }
#endif

    }


}
