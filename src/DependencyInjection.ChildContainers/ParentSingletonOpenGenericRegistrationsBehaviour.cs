namespace Dazinator.Extensions.DependencyInjection
{
    public enum ParentSingletonBehaviour
    {
        /// <summary>
        /// If there are singleton registrations in the parent container, they will be registered again in the child container as duplicate singletons. This means resolving the singleton service in the parent and child container will yield two seperate instances of that service.
        /// </summary>
        DuplicateSingletons = 1,
        /// <summary>
        /// If there are singleton registrations in the parent container, they will be omitted from the child container. In other words you won't be able to resolve these services from the built child container.
        /// </summary>
        Omit = 2,
        /// <summary>
        /// If there are singleton registrations in the parent container, requests to the child container for instances of those service types will be redirected to the parent container - keeping a single instance accross the application. This involves a runtime dictionary lookup when services are resolved so may introduce a small performance decrement.
        /// </summary>
        Delegate = 3
    }
}
