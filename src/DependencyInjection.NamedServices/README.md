## Named Services

Allows you to register services that can be resolved by name.

```csharp
    var services = new ServiceCollection();
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton(); // defaults to name of String.Empty
        names.AddSingleton("A"); 
        names.AddSingleton<BearService>("B");
        names.AddSingleton("C", new BearService());
        names.AddSingleton("D", new BearService() { SomeProperty = true });
        names.AddSingleton("FOO", (sp) => new BearService());            
        
        names.AddTransient("E");
        names.AddTransient<LionService>("F");
        names.AddTransient("BAR", (sp) => new LionService());   

        names.AddScoped("G");
        names.AddScoped<DisposableTigerService>("H");
        names.AddScoped("BAZ", (sp) => new DisposableTigerService());   

    });

```

You can then do any of the following to obtain a named instance of a service:

- inject `Func<string, AnimalService>` and invoke it to obtain a named instance.
- inject `NamedServiceResolver<AnimalService>` and invoke it to obtain a named instance. (if you don't mind your services having a dependency on this library).
- if using `IServiceProvider` directly, use `sp.GetNamed<AnimalService>("A")` to obtain a named instance.

For example:

```csharp

public MyController(Func<string, AnimalService> namedServices)
{
   AnimalService serviceA = namedServices("A");
   AnimalService serviceB = namedServices("B"); // BearService derives from AnimalService
}

```

or

```csharp
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

Singletons that implement `IDisposable`, and are registered by type or a factory function, will be disposed of automatically when the named registry itself is disposed (which is also registered as a singleton with your application container).
However if you register a named singleton by instance instead of by type, then you must specify if you want that instance to be disposed for you at the point of registering it. The default assumes you will manage / own the disposal of that instance yourself.

```csharp
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton("A"); // AnimalService will be disposed for you when the named registry (singleton) is disposed if it implements `IDisposable`
        names.AddSingleton<BearService>("B"); // same as above
        names.AddSingleton("C", sp=>new BearService()); // same as above
        names.AddSingleton("D", new BearService()); // WON'T BE DISPOSED FOR YOU AS YOU OWN THIS INSTANCE'
        names.AddSingleton("D", new BearService(), registrationOwnsInstance: true); // you provided an instance, but you allow the named registry to own it. It will dispose of it for you.
    }

```

## Transient

Named transients behave as you would expect - i.e each resolution obtains a new instance.

```csharp
    var services = new ServiceCollection();
    services.AddNamed<AnimalService>(names =>
    {
        names.AddTransient("E"); // each resolution yields a new instance
        names.AddTransient<LionService>("F"); // same as above, this time we are returning an instance of a derived class whose constrcutor will be resolved / activated by the DI container.
        names.AddTransient("G", sp=>new BearService()); // same as above
   });
```

### Disposal

Named transients are not disposed of automatically. If they implement `IDisposable` then it's down to you to dispose of them appropriately.


## Scoped

Named scoped services behave as you would expect - i.e each resolution of the same named service, from the same scope, will yield the same instance.

### Disposal

All instances of named scoped services that implement `IDisposable` will automatically be disposed for you, when the current scope is disposed.


## Nameless Registrations 

Whilst registering named services, there are overloads so that you can also register a named service **without specifying any name** which may seem counter intuitive at first.
These overloads do the equivalent of using `string.Empty` for the name - so you can only do it once otherwise you'll get a duplicate key exception.

For example: :

```csharp
    
    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton();
        names.AddSingleton(""); // this would throw, you must now register any additional variations of this service with a unique name (something other than string.Empty in this case).
    }

```

Why would you do this? 
When you register a named service without a name:

1. The registration is special. The registration is promoted up into the `IServiceCollection` - which means you can now use ordinary DI to inject that `TService` as normal - i.e without any name.
2. In addition to the above, you can also still inject and resolve that service as if it were also a named service, just using `string.Empty` as the name.

This hopefully demontrates the above behaviour a bit more:


```csharp

    services.AddNamed<AnimalService>(names =>
    {
        names.AddSingleton();
        names.AddSingleton("A");
    }

    // and..

    /// AnimalService 
    public MyController(AnimalService defaultService, NamedServiceResolver<AnimalService> namedServices)
    {
       // defaultService has been injected by ordinary DI it correlates with the `nameless registration` we made.
       // However it can also be resolved using namedServices with an empty string as it's name.
       Assert.Same(defaultService, namedServices[string.Empty]);
    }


```

The main reason for this feature was just so that there is one place to register all the variations of your service, and you don't have to switch between registering on the IServiceCollection seperately - so it's a convenience, in part.

## Forwarded Names

Suppose you register a service named "OldName".

In places around your code base, you'll end up with code like this:

```csharp
 var serviceA = namedServices("OldName");

```

This works, but later you want to change the name the service is registered with on startup:

```csharp
 namedServices.AddSingleton("AwesomePaymentService", new PaymentService());

```

Suddenly code in libraries may break as they fail to resolve the service using the old name. You may have been through the code base and renamed all the names but perhaps the code in question is in a library? Or perhaps your solution is huge and you don't want to change this.

You can now workaround this by using the `ForwardName` API to forward a name to another name:


```csharp
 namedServices.AddSingleton("AwesomePaymentService", new PaymentService());
 namedServices.ForwardName("OldName", "AwesomePaymentService");

```

Now all your consumer code can continue to request the service with "OldName" but the resolution will be forwarded to the service you've registered as "AwesomePaymentService". New code can just use "AwesomePaymentService" when requesting the service if you think thats helpful - that's really for you to decide - as it may just result in a another mapping having to be added in future :-)

## Late Registrations

In some particularly dynamic applications, it may not be known on application startup what the entire list of named services is that you will need.

For example, in an application where you create new tenants at runtime, suppose you want each tenant to be able to have a service that can be retreived based on the tenants ID or unique name. 

`LateRegistrations` let's you provide a delegate as a fallback so that when a named service is requested, and no registration for that name already exists, your delegate will fire and be given a chance to create a registration for that name that will then be used for all susequent requests.

For example:

```csharp

      var requestsMade = new List<string>();
      services.AddNamed<DatabaseService>(names =>
      {
          names.AddSingleton("A", instance);
          names.AddLateRegistration((name, factory) =>
          {
              // Capturing the name that was requested for test assertions..
              requestsMade.Add(name);

              if (name.StartsWith("TenantID:"))
              {
                  return factory.Result((a) => a.Create<TenantDatabaseService>(ServiceLifetime.Scoped));
              };

              if (name.StartsWith("AppId:"))
              {
                  return factory.Result((a) => a.Create((sp) => new AppDatabaseService(), ServiceLifetime.Scoped));
              }

              if (name.StartsWith("System:"))
              {
                  return factory.Result(a => a.Create<SystemDatabaseService>(ServiceLifetime.Singleton));
              }

              // Don't have to create a new registration, you can also map requests with this name to an existing name:
              if(name.StartsWith("AB"))
              {
                  // don't register a new service, just use existing registered service named "A".
                  return factory.Result(null, forwardToName: "A");
              }                  
            
              // You don't have to satisfy this request, you can return null, in which case the caller will get `KeyNotFoundException` at the callsite when requesting the service with the name.
              return null; //nah
          });
      });

```

## Advanced Patterns

Depending upon your case, you may be able to use the following technique when registering services, to wire them up with particular named dependencies,
to avoid the sort of code leaking into the classes themselves where they are having to "request" (locate) a service with a specific name - keeping your services completely oblivious to the fact that they are using "named" dependencies at all:

```csharp
    var services = new ServiceCollection();
    // register your named variations / flavours of some dependency:
    services.AddNamed<Claws>(names =>
    {
        names.AddTransient("D");
        names.AddScoped("E", (sp)=>new SharpClaws());
    });

    // register your services, and wire them up with the named variation of the dependency that they need, explicitly.   
    services.AddTransient<Bear>(sp=>new HungryBear(sp.GetNamed<Claws>("D")));
    services.AddTransient<Bear>(sp=>new HungryBear(sp.GetNamed<Claws>("E")));

    // later.. 
    var bears == sp.GetRequiredService<IEnumerable<Bear>>();
    // bears contains 1x LazyBear with `Claws` and 1x HungryBear with `SharpClaws` (Claws are Transiently created, where as SharpClaws are Scoped)

```

Note: You should be careful though, regarding the following:

- You don't want to allow services that are registered as `singleton`, to be handed dependencies that are registered as `scoped`.
- You don't want to call `sp.GetNamed<Claws>("D")` to obtain a transient if its `IDisposable` as that instance will not be disposed for you when using the above technique. 

You can workaround the disposal issue by doing something like:

```csharp
services.AddTransient<Bear>(sp=> { 
  var disposableClaws = sp.GetNamed<Claws>("E");
  Action onDispose = ()=> disposableClaws.Dispose(); // ensure the named IDisposable gets disposed.
  var service = new HungryBear(disposableClaws, onDispose); 

  // HungryBear service must implement `IDisposable` and call the onDispose action when it's disposed for this pattern to work.
  return service;
});

```

The above pattern basically means:

1. The service (in this case `HungryBear`) doesn't need to know its working with a `named` dependency - so it doesn't need to request / resolve the dependency with a given name and just uses ordinary DI.
2. The service (`HungryBear`) doesn't need to care about disposing of the named depenedencies directly. This adheres to the standard pattern in that when IDisposables are directly injected into a service, its not for that service to dispose of them. However 
it does need to implement IDisposable and call that callback on disposal, to make sure its dependencies are disposed - so the act of disposing the injected service is now indirectly being done via the callback. This is less of a code smell than having the service call Dispose on its dependency directly as the service doesn't need to know what the callback is doing.
