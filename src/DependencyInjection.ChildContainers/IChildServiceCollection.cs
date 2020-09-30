namespace Dazinator.Extensions.DependencyInjection.ChildContainers
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// An <see cref="IServiceCollection"/> that provides access to <see cref="ServiceDescriptor"/> added in a parent <see cref="IServiceCollection"/> in addition to those added directly to itself.
    /// </summary>
    public interface IChildServiceCollection : IServiceCollection
    {
        IEnumerable<ServiceDescriptor> ChildDescriptors { get; }
        IEnumerable<ServiceDescriptor> ParentDescriptors { get; }
        IChildServiceCollection RemoveParentDescriptors(Func<ServiceDescriptor, bool> parentFilterPredicate);

        IChildServiceCollection RemoveParentDescriptors(Type serviceType);

        /// <summary>
        /// Calls to <see cref="Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions.TryAdd"/> within the <paramref name="configureServices"/> will not be prevented from succeeding if descriptors for the same service exist in parent services.
        /// - Resulting in potentially duplicate registrations being added to child service descriptors. In addition, any such
        /// duplicate service registrations will be detected, and then removed from Parent level service descriptors - effectively promoting the service descriptor to a "child only" registration.
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="configureServices"></param>
        /// <returns></returns>
        IChildServiceCollection AutoPromoteChildDuplicates(Func<ServiceDescriptor, bool> predicate,
                       Action<IChildServiceCollection> configureServices, Func<ServiceDescriptor, bool> promotePredicate = null);

        /// <summary>
        /// Configure the current collection, allows for chaining.
        /// </summary>
        /// <returns></returns>
        IChildServiceCollection ConfigureServices(Action<IServiceCollection> configureServices);
        
    }
}
