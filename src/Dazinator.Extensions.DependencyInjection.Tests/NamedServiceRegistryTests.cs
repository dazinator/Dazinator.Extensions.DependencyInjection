using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices.WindowsRuntime;
using Xunit;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public class NamedServiceRegistryTests
    {

        [Fact]
        public void Can_Add_Named_Instances()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new AnimalService() { SomeProperty = "B" });

            var registeredA = namedRegistrations.GetRegistration("A");
            var registeredB = namedRegistrations.GetRegistration("B");

            Assert.NotNull(registeredA.Instance);
            Assert.Equal(Lifetime.Singleton, registeredA.Lifetime);
            Assert.NotNull(registeredB.Instance);
            Assert.Equal(Lifetime.Singleton, registeredB.Lifetime);
        }

        [Fact]
        public void Can_Add_Named_Instances_Of_Derived_Types()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new BearService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new LionService() { SomeProperty = "B", SomeOtherProperty = true });


            var registeredA = namedRegistrations.GetRegistration("A");
            var registeredB = namedRegistrations.GetRegistration("B");

            Assert.NotNull(registeredA.Instance);
            Assert.IsType<BearService>(registeredA.Instance);
            Assert.Equal(Lifetime.Singleton, registeredA.Lifetime);

            Assert.NotNull(registeredB.Instance);
            Assert.IsType<LionService>(registeredB.Instance);
            Assert.Equal(Lifetime.Singleton, registeredB.Lifetime);
        }

        [Fact]
        public void Can_Add_Named_Types()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A");
            namedRegistrations.AddSingleton<BearService>("B");

            var registeredA = namedRegistrations.GetRegistration("A");

            Assert.Null(registeredA.Instance);
            Assert.NotNull(registeredA.Type);
            Assert.Equal(typeof(AnimalService), registeredA.Type);
            Assert.Equal(Lifetime.Singleton, registeredA.Lifetime);

            var registeredB = namedRegistrations.GetRegistration("B");

            Assert.Null(registeredB.Instance);
            Assert.NotNull(registeredB.Type);
            Assert.Equal(typeof(BearService), registeredB.Type);
            Assert.Equal(Lifetime.Singleton, registeredB.Lifetime);

        }

        [Fact]
        public void Can_Add_MixtureOf_Instances_And_Types()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>();
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new LionService() { SomeProperty = "B", SomeOtherProperty = true });
            namedRegistrations.AddSingleton<TigerService>("C");

            var registeredA = namedRegistrations.GetRegistration("A");

            Assert.NotNull(registeredA.Instance);
            Assert.IsType<AnimalService>(registeredA.Instance);

            var registeredB = namedRegistrations.GetRegistration("B");

            Assert.NotNull(registeredB.Instance);
            Assert.IsType<LionService>(registeredB.Instance);

            var registeredC = namedRegistrations.GetRegistration("C");

            Assert.Null(registeredC.Instance);
            Assert.Equal(typeof(TigerService), registeredC.Type);

        }

        [Fact]
        public void Can_Dispose_Of_SingletonInstances()
        {
            var disposable = new DisposableTigerService() { SomeProperty = "B" };

            using (var namedRegistrations = new NamedServiceRegistry<AnimalService>())
            {
                namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
                namedRegistrations.AddSingleton("B", disposable);
            }

            Assert.True(disposable.WasDisposed);
        }

        [Fact]
        public void Registry_Is_Disposed_When_Container_Is_Disposed()
        {
            var disposable = new DisposableTigerService() { SomeProperty = "B" };

            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(registry =>
            {
                registry.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
                registry.AddSingleton("B", disposable);
            });

            var sp = services.BuildServiceProvider();
            // we must resolve NamedServiceRegistry through the container atleast once,
            // otherwise the container will think it has never created an instance and therefore it won't call Dispose() on it.
            var registry = sp.GetRequiredService<NamedServiceRegistry<AnimalService>>();
            sp.Dispose();

            Assert.True(disposable.WasDisposed);
        }
    }

    public class AnimalService
    {
        public string SomeProperty { get; set; }
    }

    public class LionService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class TigerService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class BearService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }

    public class DisposableTigerService : AnimalService, IDisposable
    {
        public bool WasDisposed { get; set; } = false;
        public void Dispose()
        {
            WasDisposed = true;
        }
    }
}
