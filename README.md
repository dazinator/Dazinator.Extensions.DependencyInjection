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

Allows you to register services that can be resolved by name.

For more detailed docs [see here](./src/DependencyInjection.NamedServices/README.md)

## Child Containers

For more detailed docs [see here](./src/DependencyInjection.ChildContainers/README.md)

Allows you to configure "child containers" using the normal `IServiceCollection` interface, and also comes with a default child service provider which is just the native `ServiceProvider` implementation from microsoft.

This means there is no need to adopt a third party DI container just to gain "child containers" feature - if thats your only reason for switching DI containers.

It also means, thanks to a standard interface for building / configuring child containers, you can take a DI container library that doesn't have a child container feature,
(like I did with Microsofts) and create "child containers" with it! The caveat is that:

    - Your DI container of choice must support building a container from an `IServiceCollection` or IEnumerable<ServiceDescriptor>`
    
If you are interested in that, look at the tests for `ChildServiceCollection`

For docs, [see here](./src/DependencyInjection.ChildContainers/README.md)
