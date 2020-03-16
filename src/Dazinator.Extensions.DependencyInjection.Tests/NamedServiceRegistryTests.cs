namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using Dazinator.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using Xunit;

    public class NamedServiceRegistryTests
    {

        #region Singleton
        [Fact]
        public void Can_Add_Singleton_Instance_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider());
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new AnimalService() { SomeProperty = "B" });
            namedRegistrations.AddSingleton("C", new BearService() { SomeProperty = "C" });
            namedRegistrations.AddSingleton("D", new LionService() { SomeProperty = "D", SomeOtherProperty = true });
            namedRegistrations.AddSingleton("E", new LionService() { SomeProperty = "E" }, registrationOwnsInstance: true);

            var registeredA = namedRegistrations["A"];
            var registeredB = namedRegistrations["B"];

            Assert.NotNull(registeredA);
            Assert.Equal(Lifetime.Singleton, registeredA.Lifetime);
            Assert.False(registeredA.RegistrationOwnsInstance);
            Assert.Null(registeredA.ImplementationType);

            Assert.NotNull(registeredB);
            Assert.Equal(Lifetime.Singleton, registeredB.Lifetime);
            Assert.False(registeredB.RegistrationOwnsInstance);
            Assert.Null(registeredB.ImplementationType);

            var registeredC = namedRegistrations["C"];
            var registeredD = namedRegistrations["D"];

            Assert.NotNull(registeredC);
            Assert.Equal(Lifetime.Singleton, registeredC.Lifetime);
            Assert.False(registeredC.RegistrationOwnsInstance);
            Assert.Null(registeredC.ImplementationType);

            Assert.NotNull(registeredD);
            Assert.Equal(Lifetime.Singleton, registeredD.Lifetime);
            Assert.False(registeredD.RegistrationOwnsInstance);
            Assert.Null(registeredD.ImplementationType);

            var registeredE = namedRegistrations["E"];
            Assert.True(registeredE.RegistrationOwnsInstance);

        }

        [Fact]
        public void Can_Add_Singleton_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider());
            namedRegistrations.AddSingleton("A");
            namedRegistrations.AddSingleton<BearService>("B");

            var registeredA = namedRegistrations["A"];

            Assert.NotNull(registeredA);
            Assert.Equal(Lifetime.Singleton, registeredA.Lifetime);
            Assert.True(registeredA.RegistrationOwnsInstance);
            Assert.Equal(typeof(AnimalService), registeredA.ImplementationType);

            var registeredB = namedRegistrations["B"];

            Assert.NotNull(registeredB);
            Assert.Equal(Lifetime.Singleton, registeredB.Lifetime);
            Assert.True(registeredB.RegistrationOwnsInstance);
            Assert.Equal(typeof(BearService), registeredB.ImplementationType);
        }

        [Fact]
        public void Can_Add_MixtureOf_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider());
            namedRegistrations.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
            namedRegistrations.AddSingleton("B", new LionService() { SomeProperty = "B", SomeOtherProperty = true });
            namedRegistrations.AddSingleton<TigerService>("C");
            namedRegistrations.AddSingleton("D");

            Assert.NotNull(namedRegistrations["A"]);
            Assert.NotNull(namedRegistrations["B"]);
            Assert.NotNull(namedRegistrations["C"]);
        }

        [Fact]
        public void Can_Dispose_Of_SingletonInstances()
        {
            var disposable = new DisposableTigerService() { SomeProperty = "Should be disposed." };
            var disposableShouldNotBeDisposed = new DisposableTigerService() { SomeProperty = "Should NOT be disposed." };

            using (var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider()))
            {
                namedRegistrations.AddSingleton("A", disposableShouldNotBeDisposed);
                namedRegistrations.AddSingleton("B", disposable, registrationOwnsInstance: true); // This param defaults to false, must specifiy true if you want instance to be disposed by service provider dispose().
            }

            Assert.False(disposableShouldNotBeDisposed.WasDisposed);
            Assert.True(disposable.WasDisposed);
        }

        [Fact]
        public void Owned_Singleton_Instances_Are_Disposed_When_ServiceProvider_Is_Disposed()
        {
            var disposable = new DisposableTigerService() { SomeProperty = "B" };

            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(registry =>
            {
                registry.AddSingleton("A", new AnimalService() { SomeProperty = "A" });
                registry.AddSingleton("B", disposable, registrationOwnsInstance: true);
            });

            var sp = services.BuildServiceProvider();
            // before the container will dispose the registry (and thus the instances registered with the registry)
            // it must have first resolved an instance of it atleast once.
            var resolved = sp.GetRequiredService<NamedServiceRegistry<AnimalService>>();
            sp.Dispose();

            Assert.True(disposable.WasDisposed);
        }
        #endregion

        #region Transient

        [Fact]
        public void Can_Add_Transient_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider());
            namedRegistrations.AddTransient("A");
            namedRegistrations.AddTransient<BearService>("B");

            var registeredA = namedRegistrations["A"];

            Assert.NotNull(registeredA);
            Assert.Equal(Lifetime.Transient, registeredA.Lifetime);
            Assert.False(registeredA.RegistrationOwnsInstance);
            Assert.Equal(typeof(AnimalService), registeredA.ImplementationType);

            var registeredB = namedRegistrations["B"];

            Assert.NotNull(registeredB);
            Assert.Equal(Lifetime.Transient, registeredB.Lifetime);
            Assert.False(registeredB.RegistrationOwnsInstance);
            Assert.Equal(typeof(BearService), registeredB.ImplementationType);
        }

        #endregion

        #region Scoped

        [Fact]
        public void Can_Add_Scoped_Type_Registrations()
        {
            var namedRegistrations = new NamedServiceRegistry<AnimalService>(GetDefaultServiceProvider());
            namedRegistrations.AddScoped("A");
            namedRegistrations.AddScoped<BearService>("B");

            var registeredA = namedRegistrations["A"];

            Assert.NotNull(registeredA);
            Assert.Equal(Lifetime.Scoped, registeredA.Lifetime);
            Assert.False(registeredA.RegistrationOwnsInstance);
            Assert.Equal(typeof(AnimalService), registeredA.ImplementationType);

            var registeredB = namedRegistrations["B"];

            Assert.NotNull(registeredB);
            Assert.Equal(Lifetime.Scoped, registeredB.Lifetime);
            Assert.False(registeredB.RegistrationOwnsInstance);
            Assert.Equal(typeof(BearService), registeredB.ImplementationType);
        }

        #endregion

        private IServiceProvider GetDefaultServiceProvider()
        {
            var services = new ServiceCollection();
            return services.BuildServiceProvider();
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
