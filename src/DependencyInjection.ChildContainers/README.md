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

