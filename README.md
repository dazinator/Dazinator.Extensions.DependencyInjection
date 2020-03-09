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
        
        names.AddTransient("F");
        names.AddTransient<LionService>("E");
    });

```

You can now inject  `Func<string, AnimalService>` or `NamedServiceResolve` (depends if you don't mind your services having a dependency on this library or not).

Get services by name like this:

```csharp

public MyController(Func<string, AnimalService> namedServices)
{
   AnimalService serviceA = namedServices("A");
   AnimalService serviceB = namedServices("B"); // BearService derives from AnimalService
}

```

## Singletons

When you register singletons, they Singleton PER NAME.
For example:

```csharp
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton<BearService>("A"); 
        names.AddSingleton<BearService>("B");
    }
```

In this case, "A"` and "B" will resolve to two *different instances*.
However resolutions of "A" will yeild the same singleton instance, and the same for "B".

### Disposal

By default singletons that implement IDisposable, and are registered by type, will be disposed automatically when the applicatons `IServiceProvider` is disposed.
However if you register an instance, you must specify if you want the instance to be disposed for you, otherwise it is assumed you will manage disposal yourself.

```csharp
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton("A"); // AnimalService will be disposed for you if it implements IDisposable
        names.AddSingleton<BearService>("B"); // same as above
        names.AddSingleton("D", new BearService(), registrationOwnsInstance: true); // you provided an instance, you must specify - default is false.
    }

```