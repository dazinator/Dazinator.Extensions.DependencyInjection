// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Dazinator.Extensions.DependencyInjection.ServiceLookup
{
    internal class ExpressionsServiceProviderEngine : ServiceProviderEngine
    {
        private readonly ExpressionResolverBuilder _expressionResolverBuilder;
        public ExpressionsServiceProviderEngine(IEnumerable<ServiceDescriptor> serviceDescriptors, Func<IServiceProvider, IServiceProvider>? serviceProviderFactory = null)
            : base(serviceDescriptors, serviceProviderFactory)
        {
            _expressionResolverBuilder = new ExpressionResolverBuilder(RuntimeResolver, Root);
        }

        protected override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            Func<ServiceProviderEngineScope, object> realizedService = _expressionResolverBuilder.Build(callSite);
            RealizedServices[callSite.ServiceType] = realizedService;
            return realizedService;
        }
    }
}
