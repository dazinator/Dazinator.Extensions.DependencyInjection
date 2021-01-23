namespace Dazinator.Extensions.DependencyInjection
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Dazinator.Extensions.DependencyInjection.ChildContainers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

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

#if NETSTANDARD1_3
        public static IServiceProvider CreateChildServiceProvider(
#else
        public static IServiceProvider CreateChildServiceProvider(
#endif

           this IServiceCollection parentServices, IServiceProvider parentServiceProvider, Action<IChildServiceCollection> configure, ParentSingletonOpenGenericRegistrationsBehaviour behaviour = ParentSingletonOpenGenericRegistrationsBehaviour.ThrowIfNotSupportedByContainer)
        {
            var childServices = parentServices.CreateChildServiceCollection();
            configure?.Invoke(childServices);
            var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider, behaviour);
            return childContainer;
        }

#if NETSTANDARD1_3
        public static async Task<IServiceProvider> CreateChildServiceProviderAsync(
#else
        public static async Task<IServiceProvider> CreateChildServiceProviderAsync(
#endif

           this IServiceCollection parentServices, IServiceProvider parentServiceProvider, Func<IChildServiceCollection, Task> configureAsync, ParentSingletonOpenGenericRegistrationsBehaviour behaviour = ParentSingletonOpenGenericRegistrationsBehaviour.ThrowIfNotSupportedByContainer)
        {
            var childServices = parentServices.CreateChildServiceCollection();
            if (configureAsync != null)
            {
                await configureAsync(childServices);
            }
            var childContainer = childServices.BuildChildServiceProvider(parentServiceProvider, behaviour);
            return childContainer;
        }

#if NETSTANDARD1_3
        public static IServiceProvider BuildChildServiceProvider(
#else
        public static IServiceProvider BuildChildServiceProvider(
#endif

           this IChildServiceCollection childServiceCollection, IServiceProvider parentServiceProvider, ParentSingletonOpenGenericRegistrationsBehaviour singletonOpenGenericBehaviour = ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)
        {
            // add all the same registrations that are in the parent to the child,
            // but rewrite them to resolve from the parent IServiceProvider.

            var parentRegistrations = childServiceCollection.ParentDescriptors;
            var reWrittenServiceCollection = new ServiceCollection();
            var unsupportedDescriptors = new List<ServiceDescriptor>(); // we can't honor singleton open generic registrations (child container would get different instance)

            foreach (var item in parentRegistrations)
            {
                var rewrittenDescriptor = CreateChildDescriptorForExternalService(item, parentServiceProvider, unsupportedDescriptors, singletonOpenGenericBehaviour);
                if (rewrittenDescriptor != null)
                {
                    reWrittenServiceCollection.Add(rewrittenDescriptor);
                }
            }

            if (unsupportedDescriptors.Any())
            {
                if (singletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.ThrowIfNotSupportedByContainer)
                {
                    ThrowUnsupportedDescriptors(unsupportedDescriptors);
                }
            }

            // Child service descriptors can be added "as-is"
            foreach (var item in childServiceCollection.ChildDescriptors)
            {
                reWrittenServiceCollection.Add(item);
            }

            if (singletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)
            {
                var childSp = reWrittenServiceCollection.BuildServiceProvider();
                var routingSp = new ReRoutingServiceProvider(childSp);
                routingSp.ReRoute(parentServiceProvider, unsupportedDescriptors.Select(a => a.ServiceType));
                return routingSp;
            }
            else
            {
                var sp = reWrittenServiceCollection.BuildServiceProvider();
                return sp;
            }



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


        private static ServiceDescriptor CreateChildDescriptorForExternalService(ServiceDescriptor item, IServiceProvider parentServiceProvider, List<ServiceDescriptor> unsupportedDescriptors, ParentSingletonOpenGenericRegistrationsBehaviour singletonOpenGenericBehaviour)
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

            // We don't have an "open generic type" singleton service definition do we?
            // please say we don't because things will get... awkward.
            if (item.ServiceType.IsClosedType())
            {
                // oh goodie
                // get an instance of the singleton from the parent container - so its owned by the parent.
                // then register this instance as an externally owned instance in this child container.
                // child container won't then try and own it.                
                var singletonInstance = parentServiceProvider.GetRequiredService(item.ServiceType);
                var serviceDescriptor = new ServiceDescriptor(item.ServiceType, singletonInstance); // by providing the instance, the child container won't manage this object instances lifetime (i.e call Dispose if its IDisposable).
                return serviceDescriptor;
            }

            // These incompatible services should have already been filtered out of the child service collection based on
            // HideParentServices()

            if (singletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons)
            {
                // allow the open generic singleton registration to be added again to this child again resulting in additional singleton instance at child scope.
                return item;
            }

            if (singletonOpenGenericBehaviour == ParentSingletonOpenGenericRegistrationsBehaviour.Omit)
            {
                // exclude this service from the child container. It won't be able to be resolved from child container.
                return null;
            }

            unsupportedDescriptors.Add(item);
            return null;

            // oh flip
            // e.g IOptions<T>
            // If so, when we resolve IOptions<T> in the child container, we need it to resolve to an instance created in the parent container
            // BUT we can't create the instance now, as we have to wait for the concrete type param to be provided..
            // This is a bit of a dilemma because if we register a function to run when the service is requsted, the child container will steal ownership.
            // because it thinks its creating the singleton type.
            // So we need to register an instance, 


            // Thoughts and Ideas..
            // Problem is how we ensure when an open generic is resolved from the child container that we resolve the same instance as resolved from the parent when the same type params are used.


            // A) If the service is non generic, or is a closed generic type, we can create an instance from the parent container and register it in the child container so that
            // the child container will re-use the same instance, AND won't take ownership of the object

            // B) If the service is an open generic, we can't create an instance ahead of time, because the container needs to create the instance based on the type params requested.
            //    In this case there is little we can do, but we can do this:
            //       - If the open generic type is not backed by an implementation type that implements IDisposable or IAsyncDisposable then it should
            //         be safe to allow the child container to "own" it - as technically that means very little.
            //         This means we can register it as a factory func, that returns the same instance from the parent?
            // NOOOO THIS WONT WORK, because we can't register a factory func to satisfy an open generic.

            // NEXT IDEA
            // A) We capture a list of all the open generic singleton registrations
            //    We derive from ServiceProvider and overide GetService<T> (or wrap IServiceProvider and decorate the same)
            //        When a type is resolved thats assignable to an open generic type in our list,
            //            We forward the resolution to the parent service provider instead. (parent.GetRequiredService()).
            // This might not work due to BuildServiceProvider capturing services to be injected at that point, therefore GetRequiredService might not be called on our decorated ServiceProvider to resolve the open generic type as the factory was already resolved during the BuildServiceProvider() method and the factory may just be called.

            // LAST IDEA
            // We don't try to solve this in a general way, but detect specific open generic singleton registrations and try to make them work here specifically.
            // bit yuck but not sure what else to do.



            //switch (singletonOpenGenericBehaviour)
            //{
            //    case ParentSingletonOpenGenericResolutionBehaviour.ThrowNotSupportedException:
            //        unsupportedDescriptors.Add(item); // we'll record this as an unsupported descriptor so an exception is thrown to include the detail.
            //        return null;
            //    case ParentSingletonOpenGenericResolutionBehaviour.RegisterAgainAsSeperateSingletonInstancesInChildContainer:
            //        return item;
            //    case ParentSingletonOpenGenericResolutionBehaviour.Omit:
            //        return null;
            //    default:
            //        return null;
            //}       
        }
    }
}
