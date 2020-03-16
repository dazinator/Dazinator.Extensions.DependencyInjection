namespace Dazinator.Extensions.DependencyInjection.Tests
{
    using System.Dynamic;
    using Dazinator.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class NamedServiceResolverTests
    {

        #region Singleton
        [Fact]
        public void Can_Resolve_Singleton_Instance()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names => names.AddSingleton("A", new LionService()));

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver["A"];
            Assert.IsType<LionService>(singletonInstanceA);

        }

        [Fact]
        public void Can_Resolve_Singleton_Type()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names => names.AddSingleton<BearService>("A"));

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

        #endregion

        #region Transient

        [Fact]
        public void Can_Resolve_Transient_Type()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddTransient("A");
                names.AddTransient<LionService>("B");
            });

            var sp = services.BuildServiceProvider();
            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var instanceA = resolver["A"];
            var instanceB = resolver["B"];
            Assert.NotSame(instanceA, instanceB);

            Assert.IsType<AnimalService>(instanceA);
            Assert.IsType<LionService>(instanceB);

            Assert.NotSame(instanceA, resolver["A"]);
            Assert.NotSame(instanceB, resolver["B"]);
        }

        #endregion

        #region Scoped

        [Fact]
        public void Can_Resolve_Scoped_Type()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddScoped("A");
                names.AddScoped<DisposableTigerService>("B");
            });

            var sp = services.BuildServiceProvider();
            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var instanceA = resolver["A"];
            var instanceB = resolver["B"];
            Assert.NotSame(instanceA, instanceB);

            Assert.IsType<AnimalService>(instanceA);
            Assert.IsType<DisposableTigerService>(instanceB);


            // multiple resolves from same scope yields same instances.
            Assert.Same(instanceA, resolver["A"]);
            Assert.Same(instanceB, resolver["B"]);

            DisposableTigerService checkDisposed = null;
            using (var newScope = sp.CreateScope())
            {
                // initial resolve from  new scope yields new instance
                var scopedResolver = newScope.ServiceProvider.GetRequiredService<NamedServiceResolver<AnimalService>>();

                var newScopeInstanceA = scopedResolver["A"];
                var newScopeInstanceB = scopedResolver["B"];

                Assert.NotSame(newScopeInstanceA, instanceA);
                Assert.NotSame(newScopeInstanceB, instanceB);
                Assert.NotSame(newScopeInstanceA, newScopeInstanceB);

                // multiple resolves from same scope yields same instances.
                Assert.Same(newScopeInstanceA, scopedResolver["A"]);
                Assert.Same(newScopeInstanceB, scopedResolver["B"]);

                Assert.IsType<DisposableTigerService>(newScopeInstanceB);
                checkDisposed = (DisposableTigerService)newScopeInstanceB;
            }

            Assert.True(checkDisposed.WasDisposed);

            checkDisposed = (DisposableTigerService)instanceB;
            Assert.False(checkDisposed.WasDisposed);

            sp.Dispose();
            Assert.True(checkDisposed.WasDisposed);
        }

        #endregion

    }
}
