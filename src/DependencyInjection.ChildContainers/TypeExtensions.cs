namespace Dazinator.Extensions.DependencyInjection
{
    using System;

    public static class TypeExtensions
    {

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0022:Use expression body for methods", Justification = "Compilation constant makes this syntax clearer")]
        public static bool IsClosedType(this Type serviceType)
        {
#if NETSTANDARD1_3           
            return serviceType.GenericTypeArguments.Length == 0 || serviceType.IsConstructedGenericType;
#else
            return !serviceType.ContainsGenericParameters;
#endif
        }
    }

}
