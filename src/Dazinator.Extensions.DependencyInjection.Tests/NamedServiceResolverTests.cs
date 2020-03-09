using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public class NamedServiceResolverTests
    {
        [Fact]
        public void Can_Resolve_Singleton_Instance()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton("A", new LionService()); // instance
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver["A"];
            Assert.IsType<LionService>(singletonInstanceA);

        }

        [Fact]
        public void Can_Resolve_Singleton_Type()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton<BearService>("A"); // type
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver["A"];
            Assert.IsType<BearService>(singletonInstanceA);
        }

        [Fact]
        public void Singletons_Should_Be_Singleton_Per_Name()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton<BearService>("A");
                names.AddSingleton<BearService>("B");
                names.AddSingleton("C", new BearService());
                names.AddSingleton("D", new BearService());
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            Assert.NotSame(resolver["A"], resolver["B"]);
            Assert.NotSame(resolver["C"], resolver["D"]);

            Assert.NotSame(resolver["A"], resolver["C"]);
            Assert.NotSame(resolver["A"], resolver["D"]);
            Assert.NotSame(resolver["B"], resolver["C"]);
            Assert.NotSame(resolver["B"], resolver["D"]);

            Assert.Same(resolver["A"], resolver["A"]);
            Assert.Same(resolver["B"], resolver["B"]);
            Assert.Same(resolver["C"], resolver["C"]);
            Assert.Same(resolver["D"], resolver["D"]);
        }

    }
}
