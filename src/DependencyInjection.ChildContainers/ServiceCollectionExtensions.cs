namespace Dazinator.Extensions.DependencyInjection
{
    using Dazinator.Extensions.DependencyInjection.ChildContainers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using System;
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


        public static IServiceProvider BuildChildServiceProvider(this IChildServiceCollection childServiceCollection, IServiceProvider parentServiceProvider)
        {
            // add all the same registrations that are in the parent to the child,
            // but rewrite them to resolve from the parent IServiceProvider.

            var parentRegistrations = childServiceCollection.ParentDescriptors;
            var reWrittenServiceCollection = new ServiceCollection();

            foreach (var item in parentRegistrations)
            {
                var rewrittenDescriptor = CreateChildDescriptorForExternalService(item, parentServiceProvider);
                reWrittenServiceCollection.Add(rewrittenDescriptor);
            }

            // Child service descriptors can be added "as-is"
            foreach (var item in childServiceCollection.ChildDescriptors)
            {
                reWrittenServiceCollection.Add(item);
            }

            var sp = reWrittenServiceCollection.BuildServiceProvider();
            return sp;

        }

        private static ServiceDescriptor CreateChildDescriptorForExternalService(ServiceDescriptor item, IServiceProvider parentServiceProvider)
        {
            // For any services that implement IDisposable, they they will be tracked by Microsofts `ServiceProvider` when it creates them.
            // For a child container, we want the child container to be responsible for the objects lifetime, not the parent container.
            // So we must register the service in the child container. This means the child container will create the instance.
            // This should be ok for "transient" and "scoped" registrations because it shouldn't matter which container creates the instance.
            // However for "singleton" registrations, we want to assume that the singleton should be a "global" singleton - so we don't want the
            // child container to create an instance for that service by default. If the user wants the service to be "singleton at the child container level"
            // then they can add a registration to the ChildServiceCollection for this (which will override anything we do here to amend the parent level registration).
            if (item.Lifetime == ServiceLifetime.Transient || item.Lifetime == ServiceLifetime.Scoped)
            {
                return item;
            }

            if (item.ImplementationInstance != null)
            {
                // global singleton instance already provided with lifetime managed externally so can just re-use this registration.
                return item;
            }

            var singletonInstance = parentServiceProvider.GetRequiredService(item.ServiceType);
            var serviceDescriptor = new ServiceDescriptor(item.ServiceType, singletonInstance); // by providing the instance, the child container won't manage this object instances lifetime (i.e call Dispose if its IDisposable).
            return serviceDescriptor;
        }

    }

}
