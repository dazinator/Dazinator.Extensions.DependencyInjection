namespace DependencyInjection.Tests.Named
{
    using System;
    using Dazinator.Extensions.DependencyInjection;
    using Dazinator.Extensions.DependencyInjection.Tests;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ServiceCollectionRegistrationTests
    {

        [Fact]
        public void Can_AddNamedServices_UsingServiceCollectionExtensionsMethods()
        {

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<AnimalService>("Foo");
            services.AddSingleton<AnimalService, LionService>("Lion");

            var sp = services.BuildServiceProvider();


            var namedServiceFactory = sp.GetRequiredService<Func<string, AnimalService>>();
            var foo = namedServiceFactory("Foo");
            Assert.IsType<AnimalService>(foo);

            var lion = namedServiceFactory("Lion");
            Assert.IsType<LionService>(lion);
        }

        [Fact]
        public void Can_AddNamedServices_UsingServiceCollectionExtensionsMethods_And_CallbackBuilderMethod()
        {

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            services.AddSingleton<AnimalService>("Foo");
            services.AddSingleton<AnimalService, LionService>("Lion");

            services.AddNamed<AnimalService>((names) =>
            {

                names.AddTransient<LionService>("AnotherLion");
            });

            var sp = services.BuildServiceProvider();

            var namedServiceFactory = sp.GetRequiredService<Func<string, AnimalService>>();
            var foo = namedServiceFactory("Foo");
            Assert.IsType<AnimalService>(foo);

            var lion = namedServiceFactory("Lion");
            Assert.IsType<LionService>(lion);

            var anotherLion = namedServiceFactory("AnotherLion");
            Assert.IsType<LionService>(anotherLion);
        }

        [Fact]
        public void Can_AddNamedServices_UsingServiceCollectionExtensionsMethods_And_CallbackBuilderMethod_InDifferentOrder()
        {

            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            services.AddNamed<AnimalService>((names) =>
            {
                names.AddTransient<LionService>("Lion");
            });

            services.AddSingleton<AnimalService>("Foo");

            var sp = services.BuildServiceProvider();

            var namedServiceFactory = sp.GetRequiredService<Func<string, AnimalService>>();
            var foo = namedServiceFactory("Foo");
            Assert.IsType<AnimalService>(foo);

            var lion = namedServiceFactory("Lion");
            Assert.IsType<LionService>(lion);

            //  Assert.Throws<InvalidOperationException>(() => services.CollateNamed());

        }
    }
}
