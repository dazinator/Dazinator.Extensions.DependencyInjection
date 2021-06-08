namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Reflection;

    public static class TypeExtensions
    {
        public static MethodInfo GetMethod(this Type givenType, string name)
        {
#if NETSTANDARD1_3
            return givenType.GetMethod(name);
#else
            return givenType.GetMethod(name);
#endif
        }
    }
}
