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
        void ConfigureParentServices(Action<IServiceCollection> configureServices);
        void ConfigureChildServices(Action<IServiceCollection> configureServices);

        IEnumerable<ServiceDescriptor> GetParentServiceDescriptors();

        IEnumerable<ServiceDescriptor> GetChildServiceDescriptors();

        /// <summary>
        /// Any singleton services registered within the callback, will be inspected and if the same service is already registered at parent level, it will be duplicated at child level - resulting in the child getting its own singleton.
        /// The normal behaviour (when not within this callback) would be that TryAdd() for a singleton service that is already registered (i.e at parent level) would not register the same service again and the singleton would therefore remain a true singleton.
        /// </summary>
        /// <param name="configureServices"></param>
        /// <returns></returns>
        IChildServiceCollection AutoDuplicateSingletons(Action<IChildServiceCollection> configureServices);

        /// <summary>
        /// Configure the current collection, allows for chaining.
        /// </summary>
        /// <returns></returns>
        IChildServiceCollection ConfigureServices(Action<IServiceCollection> configureServices);
    }
}
