## Child Containers

Allows you to build "child containers" using the normal `IServiceCollection` interface, and backed by an `IServiceProvider` implementation of your choice.
This means there is no need to adopt a third party DI container just to gain this feature - if thats your only reason for switching. It also means that container library you are using (the implementation of IServiceProvider) need not support child container feature directly.

## Getting started

Please Note: You can also refer to the unit tests.

Add the `Dazinator.Extensions.DependencyInjection.ChildContainers` package to your project.

Note: This project is still in pre-release and the api's below subject to change:

```cs

    IServiceCollection parentServiceCollection = new ServiceCollection();
    parentServiceCollection.AddSingleton<LionService>();
    var parentServiceProvider = parentServiceCollection.BuildServiceProvider();

    var childServiceProvider = parentServiceProvider.CreateChildServiceProvider(parentServiceCollection, (childServices) =>
    {
        childServices.AddSingleton<LionService>(sp => new LionService() { SomeProperty = "child" });
    }, sp => sp.BuildServiceProvider());
  

    var parentService = parentServiceProvider.GetRequiredService<LionService>();
    var childService = childServiceProvider.GetRequiredService<LionService>();

    Assert.NotNull(parentService);
    Assert.NotNull(childService);

    Assert.NotSame(parentService, childService);
    Assert.Equal("child", childService.SomeProperty);
    Assert.NotEqual("child", parentService.SomeProperty);

```


Child containers add some complexity to your solution, but the basic idea is this:


1. If you register a service in the parent container, it can also be resolved from the child container without having to re-register it there.
2. If you register a service in the child container, it will override the registration in the parent if there is one.
3. If you register a singleton in the parent level, it's the same instance when resolved from the child container. If you want a seperate singleton instance in the child container, you need to register it as a singleton there also.

## More Detail!

With the power of child containers comes the complexity of working out where you want to register services.

Register services at different levels:
    - Parent only
    - Child only
    - Both

The below sections aims to describe the various behaviours with each of the above scenarios. 
Tests are in place to verify these behaviours.


## Parent only
- [x] Can be resolved through parent or child service provider.
- [x] are created in the correct container:
    - [x] `transient` or `scoped` services, are owned by the container they are resolved through.
        - [x] and therefore, tracked / disposed by that container if they are `IDisposable`
        - [x] can have dependencies satisfied by that container (e.g when resolving the service via the child container, dependencies can be satisfied that are only registered in the child container.
    - [x] `singleton` services are always created in the parent container. We don't want child container to create duplicate instances.

## Child only
- [x] Can not be resolved from parent
- [x] Can have dependencies satisfied from parent registrations
    - [x] If such a dependency is scoped or transient, it will be created / "owned" by the child container through which the service is resolved.
    - [x] if such a dependency is singleton, it will remain owned by the parent container (or caller if the singleton dependency was registered as an existing instance)

## Both
- [x] Resolving service using parent container results in service from parent lervel registration.
- [x] Resolving service using child container results in service from child level registration, not parent level registration (it overrides parent registration)
- [x] Resolving `IEnumerable<Service>` from the parent container should only return services relating to parent registrations in the IEnumerable.
- [x] Resolving `IEnumerable<Service>` from the child container should return services relating to both parent (inherited) and child registrations (concatenation of both)
     - This does mean its not possible to directly "replace" a parent registration, by registering it again in the child, when its resolved as IEnumerable<TService>() - both services will be yielded by the IEnumerable. Registering a service in a child container only overrides the parent registration when resolving an instance of the type, not an IEnumerable of instances for the type.

## Notes on Overriding services

If you register a service in the parent container, and in the child container, and then resolve it in the child container - you get the child instance as you'd expect.

If you instead, resolve `IEnumerable<TService>` you get both (i.e the registrations in the parent for TService, and concatenated with the registrations in the child, and the IEnumerable therefore enumerates all instances).

Therefore if you want to "replace" or "delete" a service that has been registered at parent level so that it isn't included in the `IEnumerable<TService>` that is resolved from the child container, you'll have to add your own work around in application code using something like this to filter the IEnumerable as you see fit.

```cs

public class MyService 
{

public MyService(IEnumerable<Service> services)
{

// e.g filter the IEnumerable yourself to remove or replace services sourced from parent container registrations.
this.Services = services.Where(a=>a.Type.Name=="Foo").ToList();
}

}

```

Or you can attempt to manipulate the IServiceCollection's before the containers are built.

This was a design decision - it's better to potentially include "too many" services and allow you to filter them yourself, that in is to autoamtically assume child registrations are not "additive" to parent ones, and perform auto substitution / replacement.


# Singleton Registrations

The default behaviour is that singletons registered at the parent level are true singletons, and are **not** created again when resolved from the child containe - the child container delegates to the parent container to resolve the singleton.

```csharp
  // Singleton open generic registered only in parent container
  services.AddSingleton(typeof(IOptions<>), typeof(OptionsManager<T>));

  var serviceProvider = services.BuildServiceProvider();
  var childServiceProvider = BuildChildServiceProvider(serviceProvider); 

  var instanceA = parentServiceProvider.GetRequiredService<IOptions<Program>>();
  var instanceB = childServiceProvider.GetRequiredService<IOptions<Program>>();

  Assert.Same(instanceA,instanceB);

```

This gives the default behaviour that only a single instance is ever created.

When registering services at the child level, if the service has already been registered at parent level, api's that use `services.TryAdd<TService>` will fail to add another registration at child level, as it sees that the service is already registered - it can't tell that the registration is at parent level.
Many api's provided by microsoft - like `services.AddLogging()` for example, use `TryAdd()` and so if an `ILoggerFactory` is already registered at parent level and you use `AddLogging()` again at child level - it won't register a seperate `ILoggerFactory` instance and instead the one at parent level will end up being used.

To bypass this you can use `AutoDuplicateSingletons` api, which will hide the parent level singleton registrations allowing duplicate singleton registrations at the child level.

```csharp
        [Fact]
        public void AutoPromote_AllowsDuplicatedDescriptorsToBe_PromotedToChildLevel()
        {

            // In this scenario, a service is registered in the parent,
            // and we pretend we have an external AddXyz() method we want to call on the child IServiceCollection,
            // and that AddXyz() has some logic that will TryAdd() to add the same service descriptor.
            // because the service descriptor has already been added at parent level this would shouldy result in a duplicate NOT
            // being added as the TryAdd() will fail at child level as the child sees ALL of the parent registrations.
            // Therefore we allow the user to filter the parent registrations from view, so that AddXyz doesn't see any registrations
            // at parent level, and therefore adds all of its services again at parent level.

            var parentServiceCollection = new ServiceCollection();
            parentServiceCollection.AddSingleton<LionService>();
            parentServiceCollection.AddTransient<AnimalService>();

            Assert.Equal(2, parentServiceCollection.Count);


            IChildServiceCollection sut = new ChildServiceCollection(parentServiceCollection.Clone());
            Assert.Equal(2, sut.Count);
            Assert.Equal(2, sut.GetParentServiceDescriptors().Count());
            Assert.Empty(sut.GetChildServiceDescriptors());

            // demonstrates the issue - this will not add any service because the service descriptor already visible at parent level,
            sut.TryAddSingleton<LionService>();
            Assert.Equal(2, sut.Count);
            Assert.Equal(2, sut.GetParentServiceDescriptors().Count());
            Assert.Empty(sut.GetChildServiceDescriptors());

            // Within the action below, we are hiding parent level service descriptors that match the predicate,
            // vausing them to be added again by TryAdd()
            sut = sut.AutoDuplicateSingletons((nested) =>
             {
                 // Singleton LionService should be hidden, so TryAdd() calls should succeed
                 Assert.Single(nested);
                 Assert.Single(nested.GetParentServiceDescriptors());
                 Assert.Empty(nested.GetChildServiceDescriptors());

                 nested.TryAddSingleton<LionService>();
                 Assert.Equal(2, nested.Count);
                 Assert.Single(nested.GetParentServiceDescriptors());
                 Assert.Single(nested.GetChildServiceDescriptors());
             });

            // The duplicated service descriptor should no longer be in the parent services, only in child services - it has been promoted.
            Assert.Equal(2, sut.Count);
            Assert.Single(sut.GetParentServiceDescriptors());
            Assert.Single(sut.GetChildServiceDescriptors());
        }
```

This will result in the child having a seperate singleton instance to the parent.

### Notes

A conversation about changes to MS DI discussed here: https://github.com/dotnet/runtime/issues/41050#issuecomment-698642970
