## Child Containers

Allows you to build "child containers" using the normal `IServiceCollection` interface, and backed by the native `ServiceProvider` implementation from microsoft.
This means there is no need to adopt a third party DI container just to gain this feature - if thats your only reason for switching.

## Getting started

Please Note: You can also refer to the unit tests.

Add the `Dazinator.Extensions.DependencyInjection.ChildContainers` package to your project.

Note: This project is still in pre-release and the api's below subject to change:

```cs

    var services = new ServiceCollection();
    services.AddTransient<LionService>();

    var serviceProvider = services.BuildServiceProvider();
    var childServiceProvider = services.CreateChildServiceCollection()
                                       .ConfigureServices(child=>childServices.AddTransient<LionService>(sp => new LionService() { SomeProperty = "child" }))
                                       .BuildChildServiceProvider(appServices);    

    var parentService = serviceProvider.GetRequiredService<LionService>();
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

Please also make sure to read section titled "Known Limitations" below.

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


# Known Limitations

## Singleton Open Generic Registrations in Parent Container - Not Supported

When singleton open generic types are registered in the parent container (and Microsoft.Extensions.Hosting adds a bunch by default):

```
services.AddSingleton(typeof(IOptions<>), typeof(OptionsManager<T>));

```

The ideal behaviour (and sadly one that does not currently work) would be that the same instance is returned when resolving from either parent of child container when using the same type params, e.g:

```
  // Singleton open generic registered only in parent container
  services.AddSingleton(typeof(IOptions<>), typeof(OptionsManager<T>));

  var serviceProvider = services.BuildServiceProvider();
  var childServiceProvider = BuildChildServiceProvider(serviceProvider); // this will throw (by default) due to unsupported singleton open generic registration

  var instanceA = parentServiceProvider.GetRequiredService<IOptions<Program>>();
  var instanceB = childServiceProvider.GetRequiredService<IOptions<Program>>();

  Assert.Same(instanceA,instanceB); // your program won't actually reach this far as explained above will be an exception.

```

So, this currently isn't possible (will require some very "creative thinking" at the least)

The issue is that, singleton open generic type registrations, cannot have instances provided from "elsewhere" - the native ServiceProvider will always create them based on the closed type definition when the service is resolved, and because it creates the instance, it will own them, and it will be a different instance from one the parent container might already have.
So the lack of this capability is at odds with being able to supply the same instance owned from the parent container.

More discussion found here: https://github.com/dotnet/runtime/issues/41050#issuecomment-698642970

This may not be an issue for the majority of services, but it could be.
Rather than silently exhibit potentially unexpected behaviour, I thought it best to throw an exception when these registrations are encountered so that you can address this yourself in your application by using an enum to select a desired workaround behaviour.
This is the exception you will see when attempting to build a child container and the parent has singleton open generic registrations:

> Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider.GenericHostTests.Options_WorksInChildContainers(args: [""])
   Source: GenericHost.cs line 23
   Duration: 269 ms

 > Message: 
    System.NotSupportedException : Open generic types registered as singletons in the parent container are not supported when using child containers: 
    ServiceType: Microsoft.Extensions.Options.IOptions`1
    ServiceType: Microsoft.Extensions.Options.IOptionsMonitor`1
    ServiceType: Microsoft.Extensions.Options.IOptionsMonitorCache`1
    ServiceType: Microsoft.Extensions.Logging.ILogger`1
    ServiceType: Microsoft.Extensions.Logging.Configuration.ILoggerProviderConfiguration`1
    
 > Stack Trace: 
    ServiceCollectionExtensions.ThrowUnsupportedDescriptors(List`1 unsupportedDescriptors) line 73
    ServiceCollectionExtensions.BuildChildServiceProvider(IChildServiceCollection childServiceCollection, IServiceProvider parentServiceProvider) line 52
    TestHostedService.StartAsync(CancellationToken cancellationToken) line 118
    Host.StartAsync(CancellationToken cancellationToken)
    GenericHostTests.Options_WorksInChildContainers(String[] args) line 63
    --- End of stack trace from previous location where exception was thrown ---


 How to workaround? Note the call to `AutoPromoteChildDuplicates()`

 ```
   ChildContainer = Services.CreateChildServiceCollection()
                                         .AutoPromoteChildDuplicates(d => d.IsSingletonOpenGeneric(),
                                                                  (child) => child.AddOptions())
                                         .BuildChildServiceProvider(appServices);

 ```

 This wraps your child registrations, and notices any Singleton Open Generics (due to the predicate provded) that you register at child level,
 and if any of those services also exist at parent level, it removes those from the parent level descriptors in the returned IChildServiceCollection. 
 This basically means, as long as you "re-register" at child level all of the singleton open generic services, you won't get the exception when you build the container becuase you have effectively
 decided to register seperate "child level singletons" for all such services. If you miss any you will still get the exception for the ones you missed, allowing you to address the issue on a case by case basis.

 There is no perfect solution here, hopefull the enum comments above are sufficient to explain your options.

 If anyone has any bright ideas for a solution to this problem I am all ears.
