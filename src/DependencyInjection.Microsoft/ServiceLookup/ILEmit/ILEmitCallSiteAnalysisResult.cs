// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dazinator.Extensions.DependencyInjection.ServiceLookup
{
    internal readonly struct ILEmitCallSiteAnalysisResult
    {
        public ILEmitCallSiteAnalysisResult(int size) : this()
        {
            Size = size;
        }

        public ILEmitCallSiteAnalysisResult(int size, bool hasScope)
        {
            Size = size;
            HasScope = hasScope;
        }

#pragma warning disable IDE1006 // Naming Styles
        public readonly int Size;
#pragma warning restore IDE1006 // Naming Styles

#pragma warning disable IDE1006 // Naming Styles
        public readonly bool HasScope;
#pragma warning restore IDE1006 // Naming Styles

        public ILEmitCallSiteAnalysisResult Add(in ILEmitCallSiteAnalysisResult other) =>
            new ILEmitCallSiteAnalysisResult(Size + other.Size, HasScope | other.HasScope);
    }
}
