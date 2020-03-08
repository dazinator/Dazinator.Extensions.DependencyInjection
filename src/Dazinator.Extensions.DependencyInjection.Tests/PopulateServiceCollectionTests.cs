using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices.WindowsRuntime;
using Xunit;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public class PopulateServiceCollectionTests
    {
        [Fact]
        public void Can_Populate_Singleton_Type()
        {
            var registry = new NamedServiceRegistry<AnimalService>();
            registry.AddSingleton("A");

            var registration = registry.GetRegistration("A");
            Assert.Equal(typeof(AnimalService), registration?.Type);

            // Singleton type registrations should be registered in container.
            // unless they have factory function
            var services = new ServiceCollection();
            registry.PopulateServiceCollection(services);
            Assert.Contains(services, s => s.ServiceType == typeof(AnimalService) && s.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void Singleton_DefaultType_Registrations_Should_Be_Registered_With_Container()
        {
            var registry = new NamedServiceRegistry<AnimalService>();
            registry.AddSingleton("A");

            var services = new ServiceCollection();
            registry.PopulateServiceCollection(services);
            Assert.Contains(services, s => s.ServiceType == typeof(AnimalService) && s.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void Singleton_SpecifiedType_Registrations_Should_Be_Registered_With_Container()
        {
            var registry = new NamedServiceRegistry<AnimalService>();

            registry.AddSingleton<BearService>("A");

            var services = new ServiceCollection();
            registry.PopulateServiceCollection(services);
            Assert.Contains(services, s => s.ServiceType == typeof(BearService) && s.ImplementationType == typeof(BearService) && s.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void Singleton_Instance_Registration_Should_Not_Be_Registered_With_Container()
        {
            var registry = new NamedServiceRegistry<AnimalService>();

            var instance = new AnimalService();
            registry.AddSingleton("A", instance);
            var services = new ServiceCollection();
            registry.PopulateServiceCollection(services);
            Assert.DoesNotContain(services, s => (s.ServiceType == typeof(AnimalService) || s.ImplementationInstance == instance));
        }

     
    }

 

}
