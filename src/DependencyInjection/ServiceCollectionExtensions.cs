namespace Microsoft.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using Dazinator.Extensions.DependencyInjection;

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection Clone(this IServiceCollection services)
        {
            var clone = new ServiceCollection();
            clone.AddRange(services);
            return clone;
        }

        public static void AddRange(this IServiceCollection services, IEnumerable<ServiceDescriptor> items)
        {
            foreach (var item in items)
            {
                services.Add(item);
            }
        }

        public static void RemoveRange(this IServiceCollection services, IEnumerable<ServiceDescriptor> items)
        {
            foreach (var item in items)
            {
                services.Remove(item);
            }
        }
    }
}
