namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ChildContainers;
    using global::DependencyInjection.ReRouting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Creates a service collection that contains a read-only view of all the services from the parent <see cref="IServiceCollection"/> but lets you add and remove additional <see cref="ServiceDescriptor"'s that can be used for configuring a child container.
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <param name="parentServices"></param>
        /// <returns></returns>
        public static IChildServiceCollection CreateChildServiceCollection(this IServiceCollection parentServices, bool allowModifyingParentServiceCollection = false)
        {
            var childServiceCollection = new ChildServiceCollection(parentServices, null, allowModifyingParentServiceCollection);
            return childServiceCollection;
        }

        public static IServiceProvider CreateChildServiceProvider(
            this IServiceCollection parentServices,
            IServiceProvider parentServiceProvider,
            Action<IChildServiceCollection> configureChildServices,
            Func<IServiceCollection, IServiceProvider> buildSp,
            ParentSingletonBehaviour behaviour = ParentSingletonBehaviour.Delegate,
            bool allowModifyingParentServiceCollection = false
        )
        {
            var childServices = parentServices.CreateChildServiceCollection(allowModifyingParentServiceCollection);
            configureChildServices?.Invoke(childServices);
            var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider, s => buildSp(s), behaviour);
            return childContainer;
        }

        public static IServiceProvider CreateChildServiceProvider(this IServiceProvider parentServiceProvider, IServiceCollection parentServices, Action<IChildServiceCollection> configureChildServices, Func<IServiceCollection, IServiceProvider> buildChildServiceProvider, ParentSingletonBehaviour behaviour = ParentSingletonBehaviour.Delegate)
        {
            var childServices = parentServices.CreateChildServiceCollection();
            configureChildServices?.Invoke(childServices);
            var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider, s => buildChildServiceProvider(s), behaviour);
            return childContainer;
        }


        public static async Task<IServiceProvider> CreateChildServiceProviderAsync(
            this IServiceCollection parentServices, IServiceProvider parentServiceProvider, Func<IChildServiceCollection, Task> configureAsync, Func<IServiceCollection, IServiceProvider> buildSp, ParentSingletonBehaviour behaviour = ParentSingletonBehaviour.Delegate
        )
        {
            var childServices = parentServices.CreateChildServiceCollection();
            if (configureAsync != null)
            {
                await configureAsync(childServices);
            }

            var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider, s => buildSp(s), behaviour);
            return childContainer;
        }

        public static IServiceProvider BuildChildServiceProvider(
            this IChildServiceCollection childServiceCollection,
            IServiceProvider parentServiceProvider,
            Func<IServiceCollection, IServiceProvider> buildSp,
            ParentSingletonBehaviour singletonOpenGenericBehaviour = ParentSingletonBehaviour.Delegate
        )
        {
            // add all the same registrations that are in the parent to the child,
            // but rewrite them to resolve from the parent IServiceProvider.

            var parentRegistrations = childServiceCollection.GetParentServiceDescriptors();
            var reWrittenServiceCollection = new ServiceCollection();
            var routedToParentServiceDescriptors = new List<ServiceDescriptor>(); // we can't honor singleton open generic registrations (child container would get different instance)
            var parentScope = parentServiceProvider.CreateScope(); // obtain a new scope from the parent that can be safely used by the child for the lifetime of the child.

            foreach (var item in parentRegistrations)
            {
                var rewrittenDescriptor = CreateChildDescriptorForExternalService(item,
                    //parentScope.ServiceProvider,
                    routedToParentServiceDescriptors, singletonOpenGenericBehaviour);
                if (rewrittenDescriptor != null)
                {
                    reWrittenServiceCollection.Add(rewrittenDescriptor);
                }
            }

            // if (routedToParentServiceDescriptors.Any())
            // {
            //     if (singletonOpenGenericBehaviour == ParentSingletonBehaviour.ThrowIfNotSupportedByContainer)
            //     {
            //         ThrowUnsupportedDescriptors(routedToParentServiceDescriptors);
            //     }
            // }

            // Child service descriptors can be added "as-is"
            reWrittenServiceCollection.AddRange(childServiceCollection.GetChildServiceDescriptors());

            IServiceProvider innerSp = null;
            IServiceProvider childSp = null;
            if (singletonOpenGenericBehaviour == ParentSingletonBehaviour.Delegate)
            {
                childSp = buildSp(reWrittenServiceCollection);
                var routingSp = new ReRoutingServiceProvider(childSp);
                routingSp.ReRoute(parentScope.ServiceProvider, routedToParentServiceDescriptors.Select(a => a.ServiceType));
                innerSp = routingSp;
            }
            else
            {
                childSp = buildSp(reWrittenServiceCollection);
                innerSp = childSp;
            }

            // Make sure we dispose any parent sp scope that we have leased, when the IServiceProvider is disposed.
            void onDispose()
            {
                // dispose of child sp first, then dispose parent scope that we leased to support the child sp.
                DisposeHelper.DisposeIfImplemented(childSp);
                parentScope.Dispose();
            }

            ;


#if SUPPORTS_ASYNC_DISPOSE
            async Task onDisposeAsync()
            {
                // dispose of child sp first, then dispose parent scope that we leased to support the child sp.
                await DisposeHelper.DisposeAsyncIfImplemented(childSp);
                await DisposeHelper.DisposeAsyncIfImplemented(parentScope);
            }
#endif

            var disposableSp = new DisposableServiceProvider(innerSp, onDispose
#if SUPPORTS_ASYNC_DISPOSE
                , onDisposeAsync
#endif
            );

            return disposableSp;
        }


        private static void ThrowUnsupportedDescriptors(IEnumerable<ServiceDescriptor> unsupportedDescriptors)
        {
            var typesMessageBuilder = new StringBuilder();
            foreach (var item in unsupportedDescriptors)
            {
                typesMessageBuilder.AppendLine($"ServiceType: {item.ServiceType.FullName}, ImplementationType: {item.ImplementationType?.FullName ?? " "}");
            }

            throw new NotSupportedException("Open generic types registered as singletons in the parent container are not supported when using microsoft service provider for child containers: " + Environment.NewLine + typesMessageBuilder.ToString());
        }


        private static ServiceDescriptor CreateChildDescriptorForExternalService(ServiceDescriptor item, List<ServiceDescriptor> routedToParentServiceDescriptors, ParentSingletonBehaviour singletonBehaviour)
        {
            // For any services that implement IDisposable, they they will be tracked by Microsofts `ServiceProvider` when it creates them.
            // For services resolved via the child container, we want the child container to be responsible for the objects lifetime, not the parent container.
            // So we must register the service in the child container. This means the child container will create the instance.
            // This should be ok for "transient" and "scoped" registrations.
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

            // if (!item.ServiceType.IsClosedType())
            // {
            if (singletonBehaviour == ParentSingletonBehaviour.DuplicateSingletons)
            {
                // allow the open generic singleton registration to be added again to this child, resulting in additional singleton instance at child scope.
                return item;
            }

            if (singletonBehaviour == ParentSingletonBehaviour.Omit)
            {
                // exclude this service from the child container. It won't be able to be resolved from child container.
                return null;
            }
            // }

            // singleton where behaviour "ParentSingletonOpenGenericRegistrationsBehaviour.Delegate" was specified.
            routedToParentServiceDescriptors.Add(item);
            return null;
        }
    }
}
