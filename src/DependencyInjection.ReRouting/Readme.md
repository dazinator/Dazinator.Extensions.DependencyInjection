## Purpose

Let's you wrap an underlying `IServiceProvider` but allows you to intercept requests for specific service type's
and re-route those to an alternative IServiceProvider instead.

For example, this let's you keep a single `IServiceProvider' in your application, but now it can resolve certain services that are "outside" the original IServiceProvider.

This can be useful in trying to achieve child --> parent IServiceProvider delegation etc.

## Sample

See tests.
