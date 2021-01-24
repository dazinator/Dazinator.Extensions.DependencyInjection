// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dazinator.Extensions.DependencyInjection.Microsoft.ServiceLookup
{
    using System;
    using global::Microsoft.Extensions.DependencyInjection;

    internal interface IServiceProviderEngine : IServiceProvider, IDisposable, IAsyncDisposable
    {
        IServiceScope RootScope { get; }
        void InitializeCallback(IServiceProviderEngineCallback callback);
        void ValidateService(ServiceDescriptor descriptor);
    }
}
