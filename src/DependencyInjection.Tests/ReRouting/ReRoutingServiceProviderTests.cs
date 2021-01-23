namespace Dazinator.Extensions.DependencyInjection.Tests
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

    public class ReRoutingServiceProviderTests
    {

        #region Parent Only

        [Fact]
        [Description("Can construct override service provider")]
        public void Can_Construct()
        {
            var testSp = new TestDelegateServiceProvider(null);
            var sut = new ReRoutingServiceProvider(testSp);
        }

        [Fact]
        [Description("Can reroute requests for a service type to an alternative provider")]
        public void Can_ReRoute_Service()
        {

            //var defaultServices = new ServiceCollection();
            //defaultServices.AddTransient<AnimalService>();
            //var defaultSp = defaultServices.BuildServiceProvider();

            var defaultSp = new TestDelegateServiceProvider(null);
            var sut = new ReRoutingServiceProvider(defaultSp);

            var otherSp = new TestDelegateServiceProvider((type) =>
            {
                Assert.Equal(typeof(AnimalService), type);
                return new AnimalService() { SomeProperty = "other" };
            });

            sut.ReRoute(otherSp, typeof(AnimalService));

            var service = sut.GetService<AnimalService>();
            Assert.NotNull(service);
            Assert.Equal("other", service.SomeProperty);

        }

        [Fact]
        [Description("Can reroute requests for multiple different service type to an alternative provider")]
        public void Can_ReRoute_ManyServices()
        {

            //var defaultServices = new ServiceCollection();
            //defaultServices.AddTransient<AnimalService>();
            //var defaultSp = defaultServices.BuildServiceProvider();

            var defaultSp = new TestDelegateServiceProvider(null);
            var sut = new ReRoutingServiceProvider(defaultSp);

            var otherSp = new TestDelegateServiceProvider((type) =>
            {
                if (type == typeof(AnimalService))
                {
                    return new AnimalService() { SomeProperty = "other" };
                }
                if (type == typeof(LionService))
                {
                    return new LionService() { SomeProperty = "other" };
                }
                throw new ArgumentException(nameof(type));
            });

            sut.ReRoute(otherSp, typeof(AnimalService), typeof(LionService));

            var service = sut.GetService<AnimalService>();
            Assert.NotNull(service);
            Assert.Equal("other", service.SomeProperty);

            var lionService = sut.GetService<LionService>();
            Assert.NotNull(lionService);
            Assert.Equal("other", lionService.SomeProperty);

        }

        [Fact]
        [Description("Can reroute requests for multiple different service type to an alternative provider")]
        public void Can_GetNonRoutedServices()
        {

            //var defaultServices = new ServiceCollection();
            //defaultServices.AddTransient<AnimalService>();
            //var defaultSp = defaultServices.BuildServiceProvider();

            var defaultSp = new TestDelegateServiceProvider((type) =>
            {
                if (type == typeof(AnimalService))
                {
                    return new AnimalService() { SomeProperty = "default" };
                }              
                throw new ArgumentException(nameof(type));
            });

            var sut = new ReRoutingServiceProvider(defaultSp);

            var otherSp = new TestDelegateServiceProvider((type) => throw new ArgumentException(nameof(type)));

            var service = sut.GetService<AnimalService>();
            Assert.NotNull(service);
            Assert.Equal("default", service.SomeProperty);
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
