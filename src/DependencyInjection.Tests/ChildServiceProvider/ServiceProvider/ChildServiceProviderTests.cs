namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.ComponentModel;
    using System.Linq;
    using Dazinator.Extensions.DependencyInjection.ChildContainers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Xunit;

    public class ChildServiceProviderTests
    {

        #region Parent Only

        [Theory]
        [Description("Services registered just in parent, can be resolved through either parent or child container.")]
        [InlineData(ServiceLifetime.Transient)]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Singleton)]
        public void ParentService_ResolvedByParentOrChild(ServiceLifetime lifetime)
        {
            var parentServices = new ServiceCollection();
            var descriptor = new ServiceDescriptor(typeof(AnimalService), typeof(AnimalService), lifetime);
            parentServices.Add(descriptor);

            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            var parentService = parentServiceProvider.GetRequiredService<AnimalService>();
            var childService = childServiceProvider.GetRequiredService<AnimalService>();

            Assert.NotNull(parentService);
            Assert.NotNull(childService);
        }

        [Fact]
        [Description("Services registered in the parent container as singleton open generics, are not supported and should cause an exception to be thrown by default unless user specifies a behaviour flag to opt in to a workaround.")]
        public void ParentService_SingletonOpenGeneric_ThrowsByDefault()
        {
            var parentServices = new ServiceCollection();
            var descriptorA = new ServiceDescriptor(typeof(IGenericServiceA<>), typeof(GenericAnimalService<>), ServiceLifetime.Singleton);

            parentServices.Add(descriptorA);

            Assert.Throws<System.NotSupportedException>(() => new ChildServiceCollection(parentServices.ToImmutableList()));

            //var childServices = new ChildServiceCollection(parentServices.ToImmutableList());

            //var parentServiceProvider = parentServices.BuildServiceProvider();
            //var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            //var parentService = parentServiceProvider.GetRequiredService<AnimalService>();
            //var childService = childServiceProvider.GetRequiredService<AnimalService>();

            //Assert.NotNull(parentService);
            //Assert.NotNull(childService);
            //Assert.Same(parentService, childService);
        }



        [Theory]
        [Description("Created by the correct container.")]
        [InlineData(ServiceLifetime.Transient, true)]
        [InlineData(ServiceLifetime.Scoped, true)]
        [InlineData(ServiceLifetime.Singleton, false)] // singletons registered with the parent are global instances, and stay owned externally.
        public void ParentService_CreatedByCorrectContainer(ServiceLifetime lifetime, bool shouldBeCreatedInChildContainer = true)
        {
            // The way we verify which container the service is actually created in when resolved from the child container is:
            // Created In Child: We register the service in the parent container, but it has a dependency that is only registered in the
            // child container.
            //  a) and verify that it succeeds when resolved from child container, and fails with missing dependency when resolved from parent container.

            // Created In Parent: we do the same, but we register the dependency in both the parent and child container with different values.
            //  b) and verify that when we resolve the service from child container, the instance has the dependency from the parent container not the child container.
            //    -- v) additionally if the service is a singleton this means the same instance should be returned when resolved through child or parent.
            var parentServices = new ServiceCollection();

            var descriptor = new ServiceDescriptor(typeof(LionServiceWithDependency), typeof(LionServiceWithDependency), lifetime);
            parentServices.Add(descriptor);

            if (!shouldBeCreatedInChildContainer)
            {
                parentServices.AddTransient(sp => new DependencyA() { SomeIdentifier = "Parent" });
            }

            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());
            if (shouldBeCreatedInChildContainer)
            {
                // This also covers the case where a dependency is satisfied by the child container 
                childServices.AddTransient<DependencyA>();
            }

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            using (var childScope = childServiceProvider.CreateScope())
            {
                var childInstance = childScope.ServiceProvider.GetRequiredService<LionServiceWithDependency>();
                // a)
                Assert.NotNull(childInstance);
                if (!shouldBeCreatedInChildContainer)
                {
                    // b)
                    Assert.Equal("Parent", childInstance.Dependency.SomeIdentifier);

                    // c)
                    if (lifetime == ServiceLifetime.Singleton)
                    {
                        var parentInstance = parentServiceProvider.GetRequiredService<LionServiceWithDependency>();
                        Assert.Same(parentInstance, childInstance);
                    }
                }

            }

            if (shouldBeCreatedInChildContainer)
            {
                Assert.Throws<InvalidOperationException>(() => parentServiceProvider.GetRequiredService<LionServiceWithDependency>());
            }

        }


        [Theory]
        [Description("Disposed by the correct container.")]
        [InlineData(ServiceLifetime.Transient, true)]
        [InlineData(ServiceLifetime.Scoped, true)]
        [InlineData(ServiceLifetime.Singleton, false)] // singletons registered with the parent are global instances, and stay owned externally.
        public void ParentIDisposableService_DisposedByCorrectContainer(ServiceLifetime lifetime, bool shouldBeDisposedWithChild = true)
        {
            var parentServices = new ServiceCollection();
            var serviceWasDisposed = false;

            var descriptor = new ServiceDescriptor(typeof(AnimalService), (sp) => new DisposableAnimalService()
            {
                OnDispose = () => serviceWasDisposed = true
            }, lifetime);
            parentServices.Add(descriptor);


            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            using (var childScope = childServiceProvider.CreateScope())
            {
                var serviceOwnedByChild = childScope.ServiceProvider.GetRequiredService<AnimalService>();
            }

            Assert.Equal(shouldBeDisposedWithChild, serviceWasDisposed);

            if (!shouldBeDisposedWithChild)
            {
                // then should be disposed with parent
                parentServiceProvider.Dispose();
                Assert.Equal(!shouldBeDisposedWithChild, serviceWasDisposed);
            }
        }

        #endregion

        #region Child Only

        [Fact]
        [Description("Services registered just in child, can be resolved through child container and not parent container")]
        public void ChildService_OnlyResolvesWithChildContainer()
        {
            var parentServices = new ServiceCollection();
            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());

            childServices.AddTransient<AnimalService>();

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            Assert.Throws<InvalidOperationException>(() => parentServiceProvider.GetRequiredService<AnimalService>());
            var childService = childServiceProvider.GetRequiredService<AnimalService>();
            Assert.NotNull(childService);
        }

        [Fact]
        [Description("Can have dependencies resolved from parent registrations")]
        public void ChildService_DependenciesResolvedFromParentRegistrations()
        {
            // We register the dependencies in the parent
            // and the services in the child
            // then resolve the services through child and
            //    verify we are still able to resolve the services with the dependencies sourced from parent registrations.
            //    verify when disposing of the child, the dependencies it created (transient / scoped) that are IDisposable are disposed appropriately.
            var parentServices = new ServiceCollection();
            parentServices.AddTransient<DependencyA>();
            var wasDisposed = false;
            parentServices.AddScoped<DisposableDependencyA>(sp => new DisposableDependencyA() { OnDispose = () => wasDisposed = true });

            var singletonWasDisposed = false;
            parentServices.AddSingleton<DisposableDependencyB>(sp => new DisposableDependencyB() { OnDispose = () => singletonWasDisposed = true });


            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());
            childServices.AddTransient<LionServiceWithGenericDependency<DependencyA>>();
            childServices.AddScoped<LionServiceWithGenericDependency<DisposableDependencyA>>();
            childServices.AddScoped<LionServiceWithGenericDependency<DisposableDependencyB>>();

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            using (var scope = childServiceProvider.CreateScope())
            {
                var childServiceA = scope.ServiceProvider.GetRequiredService<LionServiceWithGenericDependency<DependencyA>>();
                var childServiceB = scope.ServiceProvider.GetRequiredService<LionServiceWithGenericDependency<DisposableDependencyA>>();

                Assert.NotNull(childServiceA);
                Assert.NotNull(childServiceB);
            }

            // If the dependency is IDisposable, and was registered into parent as scoped or transient,
            // it should now have been disposed as it was created in the child container scope which is now disposed.
            Assert.True(wasDisposed);

            // For a singleton dependency, it should not be disposed when child is disposed, as its owned by parent (or instance owned by user)
            Assert.False(singletonWasDisposed);
        }

        #endregion

        #region Parent and Child

        [Fact]
        [Description("Services registered in parent and in child are resolved according to the correct registration")]
        public void ParentServiceAndChildService_ResolvedToCorrectRegistration()
        {
            var parentServices = new ServiceCollection();
            parentServices.AddTransient<LionService>();

            var childServices = new ChildServiceCollection(parentServices.ToImmutableList());
            childServices.AddTransient<LionService>(sp => new LionService() { SomeProperty = "child" });

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            var parentService = parentServiceProvider.GetRequiredService<LionService>();
            var childService = childServiceProvider.GetRequiredService<LionService>();

            Assert.NotNull(parentService);
            Assert.NotNull(childService);

            Assert.NotSame(parentService, childService);
            Assert.Equal("child", childService.SomeProperty);
            Assert.NotEqual("child", parentService.SomeProperty);
        }


        [Theory]
        [Description("IEnumerable services enumerate services from both parent and child registrations")]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        [InlineData(new[] { typeof(AnimalService), typeof(GenericAnimalService<string>) }, new[] { typeof(LionService), typeof(GenericAnimalService<bool>) })]
        public void ParentServiceAndChildService_IEnumerableEnumeratesBoth(Type[] parentServiceTypes, Type[] childServiceTypes)
        {

            var parentServices = new ServiceCollection();
            foreach (var parentType in parentServiceTypes)
            {
                parentServices.AddTransient(typeof(AnimalService), parentType);
            }

            var childServices = parentServices.CreateChildServiceCollection();
            foreach (var childType in childServiceTypes)
            {
                childServices.AddTransient(typeof(AnimalService), childType);
            }

            //var parentSingletonDisposed = false;
            //parentServices.AddSingleton<AnimalService>(sp => new DisposableAnimalService() { OnDispose = () => parentSingletonDisposed = true });

            var parentServiceProvider = parentServices.BuildServiceProvider();
            var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

            var parentInstances = parentServiceProvider.GetServices<AnimalService>();
            AssertIEnumerableInstances(parentServiceTypes, parentInstances);

            var childInstances = childServiceProvider.GetServices<AnimalService>();
            var expectedTypes = parentServiceTypes.Concat(childServiceTypes).ToArray();
            AssertIEnumerableInstances(expectedTypes, childInstances);
        }

        private static void AssertIEnumerableInstances(Type[] expectedTypes, IEnumerable<AnimalService> instances)
        {
            Assert.NotNull(instances);
            Assert.IsAssignableFrom<IEnumerable<AnimalService>>(instances);

            var parentInstancesList = instances.ToList();

            Assert.Equal(expectedTypes.Count(), parentInstancesList.Count());
            var index = 0;

            foreach (var item in expectedTypes)
            {
                var instance = parentInstancesList[index];
                Assert.IsType(item, instance);
                index += 1;
            }
        }

        #endregion

    }
    public class AnimalService
    {
        public string SomeProperty { get; set; }
    }

    public class LionService : AnimalService
    {

    }

    public class GenericAnimalService<TMarker> : AnimalService, IGenericServiceA<TMarker>, IGenericServiceB<TMarker>
    {

    }

    public interface IGenericServiceA<TMarker>
    {

    }

    public interface IGenericServiceB<TMarker>
    {

    }

    public class DisposableAnimalService : AnimalService, IDisposable
    {
        public Action OnDispose { get; set; }
        public bool WasDisposed { get; set; } = false;
        public void Dispose()
        {
            WasDisposed = true;
            OnDispose?.Invoke();
        }
    }

    //public class GenericDisposableAnimalService<TMarker> : DisposableAnimalService, IDisposable
    //{

    //}

    public class LionServiceWithDependency : AnimalService
    {

        public LionServiceWithDependency(DependencyA dependency)
        {
            Dependency = dependency;
        }

        public bool SomeOtherProperty { get; set; }
        public DependencyA Dependency { get; }
    }

    public class LionServiceWithGenericDependency<TDependency> : AnimalService
    {

        public LionServiceWithGenericDependency(TDependency dependency) => Dependency = dependency;

        public bool SomeOtherProperty { get; set; }
        public TDependency Dependency { get; }
    }

    public class DependencyA
    {
        public string SomeIdentifier { get; set; }
    }

    public class DisposableDependencyA : IDisposable
    {
        public Action OnDispose { get; set; }
        public string SomeIdentifier { get; set; }
        public bool WasDisposed { get; set; } = false;
        public void Dispose()
        {
            WasDisposed = true;
            OnDispose?.Invoke();
        }
    }

    public class DisposableDependencyB : IDisposable
    {
        public Action OnDispose { get; set; }
        public string SomeIdentifier { get; set; }
        public bool WasDisposed { get; set; } = false;
        public void Dispose()
        {
            WasDisposed = true;
            OnDispose?.Invoke();
        }
    }


}
