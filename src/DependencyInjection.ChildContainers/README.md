## Child Containers

Allows you to build "child containers" using the normal `IServiceCollection` interface, and backed by the native `ServiceProvider` implementation from microsoft.
This means there is no need to adopt a third party DI container just to gain this feature - if thats your only reason for switching.

## Getting started

Please Note: You can also refer to the unit tests.

Add the `Dazinator.Extensions.DependencyInjection.ChildContainers` package to your project.


```cs

    var parentServices = new ServiceCollection();
    parentServices.AddTransient<LionService>();

    var childServices = new ChildServiceCollection(parentServices.ToImmutableList());
    childServices.AddTransient<LionService>(sp => new LionService() { SomeProperty = "child" }); // now configuring the child container.

    var parentServiceProvider = parentServices.BuildServiceProvider();
    var childServiceProvider = childServices.BuildChildServiceProvider(parentServiceProvider);

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
2. If you do also register a service in the child container, it will override and registration that happens to be in the parent for that service.
3. If you register a singleton in the parent level, it's the same instance when resolved from the child container - it's not magically a seperate singleton instance for the child container.
    - If you want a seperate singleton instance per child container, just add a a singleton registration when configuring the child container to override the parent registration.



## More Detail!

With the power of child containers comes the complexity of working out where you want to register services.

Register services at different levels:
    - Just in parent
    - Just in child
    - In Parent and in Child

The below sections aims to describe the various behaviours with each of the above scenarios. 
Tests are in place to verify these behaviours.


## Services registered just in parent
- [x]  can be resolved through parent or child service provider.
- [x] are created in the correct container:
    - [x] `transient` or `scoped` services, should be created by the container they are resolved through.
        - [x] and therefore, tracked / disposed by that container if they are IDisposable
        - [x] can have dependencies satisfied by that container (e.g can register a dependency in the child container which overrides the dependency (or lack of) in the parent container when resolving the service through child container)
    - [x] `singleton` services should be created in parent container. We don't want child container to create seperate instances (the user would have to register the service as a singleton in the child container to configure that explicitly)

## Services registered just in child
- [x] Can not be resolved from parent
- [x] Can have dependencies resolved from parent registrations
    - [x] If the dependency is scoped or transient, it will be owned by the container its resolved in (so child in this case)
    - [x] if the dependency is singleton, it will remain owned by the parent container (or user if its a registered instance)

## Services registered in parent and in child
- [x] parent resolution should get service registered with parent.
- [x] child resolution should get service registered with child and not the service registered with parent (it overrides parent registration)
    - [x] This means you always get two different instances, unless the user has registered a singleton instance, or is registering factory functions that capture shared instances.
 - [x] IEnumerable<Service>` resolution in parent should only return services from parent registrations.
 - [x] IEnumerable<Service>` resolution in child should return services from parent and child registrations (concatenated)
     - This does mean its not possible to directly "replace" a service registered in the parent, by registering it again in the child, when resolving IEnumerable<TService>() - as both will be included in the IEnumerable. Registering a service in a child container only overrides the parent registration when resolving the type, not an IEnumerable of that type.


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

This was a design decision - it's better to potentially include "too many" services and allow you to filter them yourself, that in is to autoamtically assume child registrations are not "additive" to parent ones, and perform auto substitution / replacement.
