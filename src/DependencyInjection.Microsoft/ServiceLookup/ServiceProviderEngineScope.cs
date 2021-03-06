// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dazinator.Extensions.DependencyInjection.Microsoft.ServiceLookup
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using global::Microsoft.Extensions.DependencyInjection;
    using global::Microsoft.Extensions.Internal;
    using SR = Resources.Strings;

    internal class ServiceProviderEngineScope : IServiceScope, IServiceProvider, IAsyncDisposable
    {
        // For testing only
        internal Action<object> _captureDisposableCallback;

        private List<object> _disposables;

        private bool _disposed;

        private IServiceProvider? _serviceProvider;
        private bool _initializing;

        public ServiceProviderEngineScope(ServiceProviderEngine engine)
        {
            Engine = engine;
        }

        internal Dictionary<ServiceCacheKey, object> ResolvedServices { get; } = new Dictionary<ServiceCacheKey, object>();

        public ServiceProviderEngine Engine { get; }

        // Lazily initialized as Engine constructs Root scope while it's still initializing.
        public IServiceProvider ServiceProvider => _serviceProvider ??= InitializeServiceProvider();

        object IServiceProvider.GetService(Type serviceType)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return Engine.GetService(serviceType, this);
        }

        internal object CaptureDisposable(object service)
        {
            Debug.Assert(!_disposed);

            _captureDisposableCallback?.Invoke(service);

            if (ReferenceEquals(this, service) || !(service is IDisposable || service is IAsyncDisposable))
            {
                return service;
            }

            lock (ResolvedServices)
            {
                if (_disposables == null)
                {
                    _disposables = new List<object>();
                }

                _disposables.Add(service);
            }
            return service;
        }

        public void Dispose()
        {
            List<object> toDispose = BeginDispose();

            if (toDispose != null)
            {
                for (int i = toDispose.Count - 1; i >= 0; i--)
                {
                    if (toDispose[i] is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    else
                    {
                        throw new InvalidOperationException(SR.Format(SR.AsyncDisposableServiceDispose, TypeNameHelper.GetTypeDisplayName(toDispose[i])));
                    }
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            List<object> toDispose = BeginDispose();

            if (toDispose != null)
            {
                try
                {
                    for (int i = toDispose.Count - 1; i >= 0; i--)
                    {
                        object disposable = toDispose[i];
                        if (disposable is IAsyncDisposable asyncDisposable)
                        {
                            ValueTask vt = asyncDisposable.DisposeAsync();
                            if (!vt.IsCompletedSuccessfully)
                            {
                                return Await(i, vt, toDispose);
                            }

                            // If its a IValueTaskSource backed ValueTask,
                            // inform it its result has been read so it can reset
                            vt.GetAwaiter().GetResult();
                        }
                        else
                        {
                            ((IDisposable)disposable).Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return new ValueTask(Task.FromException(ex));
                }
            }

            return default;

            static async ValueTask Await(int i, ValueTask vt, List<object> toDispose)
            {
                await vt.ConfigureAwait(false);
                // vt is acting on the disposable at index i,
                // decrement it and move to the next iteration
                i--;

                for (; i >= 0; i--)
                {
                    object disposable = toDispose[i];
                    if (disposable is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        ((IDisposable)disposable).Dispose();
                    }
                }
            }
        }

        private List<object> BeginDispose()
        {
            List<object> toDispose;
            lock (ResolvedServices)
            {
                if (_disposed)
                {
                    return null;
                }

                _disposed = true;
                toDispose = _disposables;
                _disposables = null;

                // Not clearing ResolvedServices here because there might be a compilation running in background
                // trying to get a cached singleton service instance and if it won't find
                // it it will try to create a new one tripping the Debug.Assert in CaptureDisposable
                // and leaking a Disposable object in Release mode
            }

            return toDispose;
        }

        private IServiceProvider InitializeServiceProvider()
        {
            if (Engine.ServiceProviderFactory is null)
                return this;
            // Re-entrant calls from ServiceProviderFactory for IServiceProvider will get `this`;
            if (_initializing)
                return this;

            // Stand in lock object to save an allocation.
            lock (ResolvedServices)
            {
                try
                {
                    _initializing = true;
                    return Engine.ServiceProviderFactory.Invoke(this) ?? this;
                }
                finally
                {
                    _initializing = false;
                }
            }
        }
    }
}
