| Branch  | DevOps |
| ------------- | ------------- |
| Master  | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=master)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=master) |
| Develop | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=develop)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=develop) |

| Package  | Stable | Pre-release |
| ------------- | --- | --- |
| Dazinator.Extensions.DependencyInjection  | [![Dazinator.Extensions.DependencyInjection](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection/) | [![Dazinator.Extensions.DependencyInjection](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection/) | 

## Register Named Services with Microsoft Dependency Injection

Allows you to register services that can be resolved by name.

```csharp
    var services = new ServiceCollection();
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton("A"); 
        names.AddSingleton<BearService>("B");
        names.AddSingleton("C", new BearService());
        names.AddSingleton("D", new BearService() { SomeProperty = true });
        
        names.AddTransient("E");
        names.AddTransient<LionService>("F");

        names.AddScoped("G");
        names.AddScoped<DisposableTigerService>("H");

    });

```

You can then inject  `Func<string, AnimalService>` or `NamedServiceResolver<AnimalService>` (depends if you don't mind your services having a dependency on this library or not).

Get services by name like this:

```csharp

public MyController(Func<string, AnimalService> namedServices)
{
   AnimalService serviceA = namedServices("A");
   AnimalService serviceB = namedServices("B"); // BearService derives from AnimalService
}

or

public MyController(NamedServiceResolver<AnimalService> namedServices)
{
   AnimalService serviceA = namedServices["A"];
   AnimalService serviceB = namedServices["B"]; // instance of BearService returned derives from AnimalService
}



```

## Singletons

When you register named singletons, they are Singleton PER NAMED registration.
For example:

```csharp
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton<BearService>("A"); 
        names.AddSingleton<BearService>("B");
    }
```

In this case:

- "A" and "B" will resolve to two *different instances* of `BearService`.
- All resolutions of "A" will yield the same singleton instance of "A"
- All resolutions of "B" will yield the same singleton instance of "B".

### Disposal

Singletons that implement `IDisposable`, and are registered by type, will be disposed automatically when the named registry itself is disposed (which is also registered as a singleton with your application container).
However if you register a named singleton by instance instead of by type, then you must specify if you want that instance to be disposed for you at the point of registering it. The default assumes you will manage / own the disposal of that instance yourself.

```csharp
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton("A"); // AnimalService will be disposed for you when the named registry (singleton) is disposed if it implements `IDisposable`
        names.AddSingleton<BearService>("B"); // same as above
        names.AddSingleton("D", new BearService(), registrationOwnsInstance: true); // you provided an instance, you must specify - default is false.
    }

```

## Transient

Named transients behave as you would expect - i.e each resolution obtains a new instance.

```csharp
    var services = new ServiceCollection();
    services.AddNamed<AnimalService>(names =>
    {
        names.AddTransient("E"); // each resolution yields a new instance of `AnimalService`
        names.AddTransient<LionService>("F"); // reach resolution yields new a new instance of `LionService` which derives from `AnimalService`
    });
```

### Disposal

Named transients are not disposed of automatically. If they implement `IDisposable` then it's down to you to dispose of them appropriately.


## Scoped

Named scoped services behave as you would expect - i.e each resolution of the same named service, from the same scope, will yield the same instance.

### Disposal

All instances of named scoped services that implement `IDisposable` will automatically be disposed for you, when the current scope is disposed.