// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dazinator.Extensions.DependencyInjection.ServiceLookup
{
    internal interface IServiceProviderEngineCallback
    {
        void OnCreate(ServiceCallSite callSite);
        void OnResolve(Type serviceType, IServiceScope scope);
    }
}
