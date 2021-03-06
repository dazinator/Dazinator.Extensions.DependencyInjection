// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Dazinator.Extensions.DependencyInjection.Microsoft.ServiceLookup
{
    internal class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(IServiceProvider));
        }
    }
}
