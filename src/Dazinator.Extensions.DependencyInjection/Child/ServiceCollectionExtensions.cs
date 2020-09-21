namespace Dazinator.Extensions.DependencyInjection
{
    using Dazinator.Extensions.DependencyInjection.Child;
    using Microsoft.Extensions.DependencyInjection;
    using System.Collections.Immutable;

    public static partial class ServiceCollectionExtensions
    {
        /// <summary>
        /// Creates a service collection that contains a read-only view of all the services from the parent <see cref="IServiceCollection"/> but lets you add and remove additional <see cref="ServiceDescriptor"'s that can be used for configuring a child container.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IChildServiceCollection CreateChildServiceCollection(this IServiceCollection services)
        {
            var childServiceCollection = new ChildServiceCollection(services.ToImmutableList());
            return childServiceCollection;
        }
    }

}
