// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Dazinator.Extensions.DependencyInjection.Microsoft.ServiceLookup
{
    internal class RuntimeServiceProviderEngine : ServiceProviderEngine
    {
        public RuntimeServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, Func<IServiceProvider, IServiceProvider>? serviceProviderFactory = null)
            : base(serviceDescriptors, serviceProviderFactory)
        {
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            return scope =>
            {
                Func<ServiceProviderEngineScope, object> realizedService = p => RuntimeResolver.Resolve(callSite, p);

                RealizedServices[callSite.ServiceType] = realizedService;
                return realizedService(scope);
            };
        }
    }
}
