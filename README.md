| Branch  | DevOps |
| ------------- | ------------- |
| Master  | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=master)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=master) |
| Develop | [![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Dazinator.Extensions.DependencyInjection?branchName=develop)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=12&branchName=develop) |

| Package  | Stable | Pre-release |
| ------------- | --- | --- |
| Dazinator.Extensions.DependencyInjection.NamedServices  | [![Dazinator.Extensions.DependencyInjection.NamedServices](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.NamedServices.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.NamedServices/) | [![Dazinator.Extensions.DependencyInjection.NamedServices](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.NamedServices.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.NamedServices/) | 
| Dazinator.Extensions.DependencyInjection.ChildContainers  | [![Dazinator.Extensions.DependencyInjection.ChildContainers](https://img.shields.io/nuget/v/Dazinator.Extensions.DependencyInjection.ChildContainers.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.ChildContainers/) | [![Dazinator.Extensions.DependencyInjection.ChildContainers](https://img.shields.io/nuget/vpre/Dazinator.Extensions.DependencyInjection.ChildContainers.svg)](https://www.nuget.org/packages/Dazinator.Extensions.DependencyInjection.ChildContainers/) | 


## Intro

This repository builds on `Microsoft.Extensions.DependencyInjection` to provide additional features, which currently are:

- Named Services
- Child Containers

## Named Services

Add the `Dazinator.Extensions.DependencyInjection.NamedServices` package to your project.

Allows you to register services that can be resolved by name.
For docs, [see here](./src/DependencyInjection.NamedServices/README.md)


## Child Containers

Add the `Dazinator.Extensions.DependencyInjection.ChildContainers` package to your project.

Allows you to build "child containers" using the normal `IServiceCollection` interface, and backed by the native `ServiceProvider` implementation from microsoft.
This means there is no need to adopt a third party DI container just to gain this feature - if thats your only reason for switching.

If you do use a third party container (like autofac, structuremap etc), this library makes it possible for you to 
register all the child services in a standardised way (into an `IChildServiceCollection` which extends `IServiceCollection`)
and then configure the child container from the `ServiceDescriptors` that are exposed by `IChildServiceCollection.ChildDescriptors`

For docs, [see here](./src/DependencyInjection.ChildContainers/README.md)