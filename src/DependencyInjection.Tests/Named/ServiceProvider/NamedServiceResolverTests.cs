namespace Dazinator.Extensions.DependencyInjection.Tests.Named.ServiceProvider
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Dazinator.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;
    using ServiceCollection = ServiceCollection;

    public class NamedServiceResolverTests
    {

        #region Singleton
        [Fact]
        public void Can_Resolve_Singleton_Instance()
        {
            var services = new ServiceCollection();
            var instance = new LionService();
            services.AddNamed<AnimalService>(names => names.AddSingleton("A", instance));

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver["A"];
            Assert.IsType<LionService>(singletonInstanceA);
            Assert.Same(instance, singletonInstanceA);

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
        public void Can_Resolve_Singleton_WithFactoryFunc()
        {
            var services = new ServiceCollection();
            bool wasCalled = false;

            services.AddNamed<AnimalService>(names => names.AddSingleton("A", (sp) =>
            {
                wasCalled = true;
                return new BearService();
            }));

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver["A"];
            Assert.IsType<BearService>(singletonInstanceA);
            Assert.True(wasCalled);
        }

        [Fact]
        public void Singletons_Should_Be_Singleton_Per_Name()
        {
            var services = new ServiceCollection();
            int factoryFuncInvokeCount = 0;
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton<BearService>("A");
                names.AddSingleton<BearService>("B");
                names.AddSingleton("C", new BearService());
                names.AddSingleton("D", new BearService());
                names.AddSingleton("E", (sp) =>
                {
                    Interlocked.Increment(ref factoryFuncInvokeCount);
                    return new BearService();
                });
                names.AddSingleton(new BearService()); //nameless
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            Assert.NotSame(resolver["A"], resolver["B"]);
            Assert.NotSame(resolver["A"], resolver["C"]);
            Assert.NotSame(resolver["A"], resolver["D"]);
            Assert.NotSame(resolver["A"], resolver["E"]);
            Assert.NotSame(resolver["A"], resolver[string.Empty]);

            Assert.NotSame(resolver["B"], resolver["C"]);
            Assert.NotSame(resolver["B"], resolver["D"]);
            Assert.NotSame(resolver["B"], resolver["E"]);
            Assert.NotSame(resolver["B"], resolver[string.Empty]);

            Assert.NotSame(resolver["C"], resolver["D"]);
            Assert.NotSame(resolver["C"], resolver["E"]);
            Assert.NotSame(resolver["C"], resolver[string.Empty]);

            Assert.NotSame(resolver["D"], resolver["E"]);
            Assert.NotSame(resolver["D"], resolver[string.Empty]);

            Assert.NotSame(resolver["E"], resolver[string.Empty]);

            Assert.Same(resolver["A"], resolver["A"]);
            Assert.Same(resolver["B"], resolver["B"]);
            Assert.Same(resolver["C"], resolver["C"]);
            Assert.Same(resolver["D"], resolver["D"]);
            Assert.Same(resolver["E"], resolver["E"]);
            Assert.Same(resolver[string.Empty], resolver[string.Empty]);

            Assert.Equal(1, factoryFuncInvokeCount);

            // nameless registration can also be directly resolved by ordinary sp.
            Assert.Same(resolver[string.Empty], sp.GetRequiredService<AnimalService>());
        }

        [Fact]
        public void Can_Resolve_Nameless_Singleton_Using_StringEmptyName()
        {
            var services = new ServiceCollection();
            var instance = new LionService();
            // we are registering a service, but without a name! This will promote it to an ordinary registration in the ServiceCollection
            // so that we can inject it normally, but it will also allow us to resolve it as a named service with an empty string.
            services.AddNamed<AnimalService>(names => names.AddSingleton(instance));

            var sp = services.BuildServiceProvider();
            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var singletonInstanceA = resolver[string.Empty];
            Assert.IsType<LionService>(singletonInstanceA);
            Assert.Same(instance, singletonInstanceA);

            Assert.Same(singletonInstanceA, sp.GetRequiredService<AnimalService>());
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

        [Fact]
        public void Can_Resolve_Transient_WithFactoryFunc()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddTransient("A", sp =>
                {
                    var instance = new AnimalService();
                    return instance;
                });
                names.AddTransient("B", sp =>
                {
                    var instance = new LionService();
                    return instance;
                });
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

        [Fact]
        public void Can_Resolve_Nameless_Transient()
        {
            var services = new ServiceCollection();
            // we are registering a service, but without a name! This will promote it to an ordinary registration in the ServiceCollection
            // so that we can inject it normally, but it will also allow us to resolve it as a named service with an empty string.
            services.AddNamed<AnimalService>(names => names.AddTransient((sp) => new LionService()));

            var sp = services.BuildServiceProvider();
            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var instanceA = resolver[string.Empty];
            Assert.IsType<LionService>(instanceA);

            var instanceB = sp.GetRequiredService<AnimalService>();
            Assert.IsType<LionService>(instanceB);

            Assert.NotSame(instanceA, instanceB);
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

        [Fact]
        public void Can_Resolve_Scoped_Type_With_Dependency()
        {
            var services = new ServiceCollection();

            services.AddScoped<Claws>();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddScoped<BearServiceWithDependency>("B");
            });

            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var resolver = scope.ServiceProvider.GetRequiredService<NamedServiceResolver<AnimalService>>();
                var instanceB = resolver["B"];

                Assert.IsType<BearServiceWithDependency>(instanceB);
                Assert.NotNull(((BearServiceWithDependency)instanceB).Dependency);

            }
        }

        [Fact]
        public void Can_Resolve_Scoped_WithFactoryFunc()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddScoped("A", sp =>
                {
                    var instance = new AnimalService();
                    return instance;
                });
                names.AddScoped("B", sp =>
                {
                    var instance = new DisposableTigerService();
                    return instance;
                });

                //names.AddScoped("C", sp =>
                //{
                //    return new BearServiceWithDependency())
                //});

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


        [Fact]
        public void Can_Resolve_Nameless_Scoped()
        {
            var services = new ServiceCollection();
            // we are registering a service, but without a name! This will promote it to an ordinary registration in the ServiceCollection
            // so that we can inject it normally, but it will also allow us to resolve it as a named service with an empty string.
            services.AddNamed<AnimalService>(names => names.AddScoped((sp) => new LionService()));

            var sp = services.BuildServiceProvider();
            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var instanceA = resolver[string.Empty];
            Assert.IsType<LionService>(instanceA);

            var instanceB = sp.GetRequiredService<AnimalService>();
            Assert.IsType<LionService>(instanceB);

            Assert.Same(instanceA, instanceB);
        }

        [Fact]
        public void Can_Resolve_Different_Scoped_Types()
        {
            var services = new ServiceCollection();

            services.AddNamed<AnimalService>(names =>
            {
                names.AddScoped("A");
                names.AddScoped<DisposableTigerService>("B");
            });

            services.AddNamed<IShapeService>(names =>
            {
                names.AddScoped<SquareService>("B");
            });

            var sp = services.BuildServiceProvider();

            using (var newScope = sp.CreateScope())
            {
                var animalResolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();
                var shapeResolver = sp.GetRequiredService<NamedServiceResolver<IShapeService>>();

                var animal = animalResolver["A"];
                var shape = shapeResolver["B"];

                Assert.NotNull(animal);
                Assert.NotNull(shape);
            }
        }

        [Fact]
        public void Can_Use_Named_FactoryFunc()
        {
            var services = new ServiceCollection();
            services.AddNamed<Claws>(names =>
            {
                names.AddScoped("D");
            });

            services.AddNamed<AnimalService>(names =>
            {
                // Animal Service named "A" gets wired up with Claws named "D".
                names.AddScoped("B", sp => new BearServiceWithFuncDependency(sp.GetNamedFunc<Claws>("D")));
            });

            // Service registered in normal way, gets wired up with Claws named "D".
            services.AddScoped(sp => new BearServiceWithDependency(sp.GetNamed<Claws>("D")));

            var sp = services.BuildServiceProvider();

            using (var newScope = sp.CreateScope())
            {
                var animalResolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();
                var clawsResolver = sp.GetRequiredService<NamedServiceResolver<Claws>>();

                var animal = animalResolver["B"];
                var claws = clawsResolver["D"];

                var notNamedBear = sp.GetRequiredService<BearServiceWithDependency>();

                Assert.NotNull(animal);
                Assert.NotNull(claws);
                Assert.NotNull(notNamedBear);

                Assert.IsType<BearServiceWithFuncDependency>(animal);
                var bear = (BearServiceWithFuncDependency)animal;

                Assert.Same(bear.Dependency, claws);
                Assert.Same(notNamedBear.Dependency, claws);
            }
        }


        #endregion

        #region ForwardedNames

        [Fact]
        public void Can_Forward_Names()
        {

            var services = new ServiceCollection();
            var instance = new LionService();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton("A", instance);
                names.AddSingleton("B", sp => new AnimalService());
                names.ForwardName("FOO", "A");
                names.ForwardName("BAR", "B");
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();

            var instanceA = resolver["A"];
            var instanceB = resolver["B"];

            var forwardedToA = resolver["FOO"];
            var forwardedToB = resolver["BAR"];

            Assert.Same(instanceA, forwardedToA);
            Assert.Same(instanceB, forwardedToB);

            Assert.Throws<KeyNotFoundException>(() => resolver["FoO"]); // case sensitive.
            Assert.Throws<KeyNotFoundException>(() => resolver["bar"]); // case sensitive.

        }


        #endregion

        #region DynamicLookup

        [Fact]
        public void Can_Use_Dynamic_Lookup()
        {
            var services = new ServiceCollection();
            var instance = new LionService();

            var lateRequestsMade = new List<string>();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton("A", instance);
                names.AddLateRegistration((name, factory) =>
                {
                    // Capturing the name that was requested for test assertions..
                    lateRequestsMade.Add(name);

                    if (name.StartsWith("Transient"))
                    {
                        return factory.Result((a) => a.Create(ServiceLifetime.Transient));
                    };

                    if (name.StartsWith("ScopedWithFactory"))
                    {
                        return factory.Result((a) => a.Create((sp) => new AnimalService(), ServiceLifetime.Scoped));
                    }

                    if (name.StartsWith("SingletonWithImplementationType"))
                    {
                        return factory.Result(a => a.Create<LionService>(ServiceLifetime.Singleton));
                    }

                    if (name.StartsWith("AB"))
                    {
                        // don't register a new service, just use existing registered service named "A".
                        return factory.Result(null, forwardToName: "A");
                    }

                    return null; //nah
                });
            });

            var sp = services.BuildServiceProvider();

            var resolver = sp.GetRequiredService<NamedServiceResolver<AnimalService>>();


            var scopedA = resolver["ScopedWithFactory"];
            var scopedB = resolver["ScopedWithFactory"];
            Assert.Same(scopedA, scopedB);

            Assert.Single(lateRequestsMade);
            Assert.Contains("ScopedWithFactory", lateRequestsMade);

            var transientA = resolver["Transient"];
            Assert.Contains("Transient", lateRequestsMade);
            Assert.Equal(2, lateRequestsMade.Count);

            var transientB = resolver["Transient"];
            Assert.Equal(2, lateRequestsMade.Count);
            Assert.NotSame(transientA, transientB); // transient services so should be different instances.

            var singletonA = resolver["SingletonWithImplementationType"];
            var singletonB = resolver["SingletonWithImplementationType"];
            Assert.Contains("SingletonWithImplementationType", lateRequestsMade);
            Assert.Equal(3, lateRequestsMade.Count);
            Assert.Same(singletonA, singletonB); // singleton services so should be same instances.

            // test forwarded late registration
            var forwarded = resolver["AB"]; // should be forwarded to registration named "A" which is a singleton
            var forwardedTarget = resolver["A"]; // should get same singleton
            Assert.Same(forwardedTarget, forwarded);

            Assert.Equal(4, lateRequestsMade.Count);
            Assert.Contains("AB", lateRequestsMade);

        }

        #endregion

    }

    public interface IShapeService
    {

    }

    public class SquareService : IShapeService
    {

    }

    public class BearServiceWithDependency : AnimalService
    {
        private readonly Claws _dependency;

        public BearServiceWithDependency(Claws dependency)
        {
            Dependency = dependency;
        }
        public Claws Dependency { get; set; }
    }

    public class Claws
    {

    }

    public class BearServiceWithFuncDependency : AnimalService
    {
        public BearServiceWithFuncDependency(Func<Claws> factory)
        {
            Dependency = factory();
        }
        public Claws Dependency { get; set; }
    }


}
