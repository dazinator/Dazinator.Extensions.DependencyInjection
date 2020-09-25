namespace Dazinator.Extensions.DependencyInjection
{
    public enum ParentSingletonOpenGenericRegistrationsBehaviour
    {
        /// <summary>
        /// If there are singleton open generic registerations in the parent container then an exception will be thrown when creating the child container. This is because there is currnetly no way to have the child container resolve the same instance of those as when resolved through the parent container. From the exception you can see a list of these unsupported registrations and then work out how to handle them.
        /// </summary>
        ThrowNotSupportedException = 0,
        /// <summary>
        /// If there are singleton open generic registerations in the parent container, they will also be registered again in the child container as seperate singletons. This means resolving an open generic type with the same type parameters in the parent and child container will yield two seperate instances of that service.
        /// </summary>
        DuplicateSingletons = 1,
        /// <summary>
        /// If there are singleton open generic registerations in the parent container, they will be omitted from the child container. In other words you'll have to register them into the child container yourself otherwise they will fail to resolve from the child container.
        /// </summary>
        Omit = 2
    }
}
