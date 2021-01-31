// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dazinator.Extensions.DependencyInjection.Microsoft.ServiceLookup
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Microsoft.Extensions.DependencyInjection;

    internal abstract class ServiceProviderEngine : IServiceProviderEngine, IServiceScopeFactory
    {
        private IServiceProviderEngineCallback _callback;

        private readonly Func<Type, Func<ServiceProviderEngineScope, object>> _createServiceAccessor;

        private bool _disposed;

        private bool _initializing;
        private IServiceScopeFactory? _serviceScopeFactory;
        private readonly object _serviceScopeFactoryInitLock = new object();

        protected ServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, Func<IServiceProvider, IServiceProvider>? serviceProviderFactory)
        {
            _createServiceAccessor = CreateServiceAccessor;
            ServiceProviderFactory = serviceProviderFactory;
            Root = new ServiceProviderEngineScope(this);
            RuntimeResolver = new CallSiteRuntimeResolver();
            CallSiteFactory = new CallSiteFactory(serviceDescriptors);
            CallSiteFactory.Add(typeof(IServiceProvider), new ServiceProviderCallSite());
            CallSiteFactory.Add(typeof(IServiceScopeFactory), new ServiceScopeFactoryCallSite());
            RealizedServices = new ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>>();
            // _serviceScopeFactory = new Lazy<IServiceScopeFactory>(InitializeServiceScopeFactory);
        }

        internal Func<IServiceProvider, IServiceProvider>? ServiceProviderFactory;

        internal ConcurrentDictionary<Type, Func<ServiceProviderEngineScope, object>> RealizedServices { get; }

        internal CallSiteFactory CallSiteFactory { get; }

        protected CallSiteRuntimeResolver RuntimeResolver { get; }

        public ServiceProviderEngineScope Root { get; }

        public IServiceScope RootScope => Root;

        // Lazily initialized as Engine is inherited and is usually not fully constructed yet.
        public IServiceScopeFactory ServiceScopeFactory => _serviceScopeFactory ??= InitializeServiceScopeFactory();

        void IServiceProviderEngine.InitializeCallback(IServiceProviderEngineCallback callback)
        {
            _callback = callback;
        }

        public void ValidateService(ServiceDescriptor descriptor)
        {
            if (descriptor.ServiceType.IsGenericType && !descriptor.ServiceType.IsConstructedGenericType)
            {
                return;
            }

            try
            {
                ServiceCallSite callSite = CallSiteFactory.GetCallSite(descriptor, new CallSiteChain());
                if (callSite != null)
                {
                    _callback?.OnCreate(callSite);
                }
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Error while validating the service descriptor '{descriptor}': {e.Message}", e);
            }
        }

        public object GetService(Type serviceType) => GetService(serviceType, Root);

        protected abstract Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite);

        public void Dispose()
        {
            _disposed = true;
            Root.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return Root.DisposeAsync();
        }

        internal object GetService(Type serviceType, ServiceProviderEngineScope serviceProviderEngineScope)
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            Func<ServiceProviderEngineScope, object> realizedService = RealizedServices.GetOrAdd(serviceType, _createServiceAccessor);
            _callback?.OnResolve(serviceType, serviceProviderEngineScope);
            DependencyInjectionEventSource.Log.ServiceResolved(serviceType);
            return realizedService.Invoke(serviceProviderEngineScope);
        }

        IServiceScope IServiceScopeFactory.CreateScope()
        {
            if (_disposed)
            {
                ThrowHelper.ThrowObjectDisposedException();
            }

            return new ServiceProviderEngineScope(this);
        }

        private Func<ServiceProviderEngineScope, object> CreateServiceAccessor(Type serviceType)
        {
            ServiceCallSite callSite = CallSiteFactory.GetCallSite(serviceType, new CallSiteChain());
            if (callSite != null)
            {
                DependencyInjectionEventSource.Log.CallSiteBuilt(serviceType, callSite);
                _callback?.OnCreate(callSite);
                return RealizeService(callSite);
            }

            return _ => null;
        }

        private IServiceScopeFactory InitializeServiceScopeFactory()
        {
            if (ServiceProviderFactory is null)
                return this;
            // Re-entrant calls from ServiceProviderFactory for IServiceScopeFactory will get `this`;
            if (_initializing)
                return this;

            lock (_serviceScopeFactoryInitLock)
            {
                try
                {
                    _initializing = true;
                    return ServiceProviderFactory.Invoke(Root)?.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory ?? this;
                }
                finally
                {
                    _initializing = false;
                }
            }
        }
    }
}
