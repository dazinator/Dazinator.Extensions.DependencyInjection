namespace Dazinator.Extensions.DependencyInjection.Child
{
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// An <see cref="IServiceCollection"/> that provides access to <see cref="ServiceDescriptor"/> added in a parent <see cref="IServiceCollection"/> in addition to those added directly to itself.
    /// </summary>
    public interface IChildServiceCollection : IServiceCollection
    {

    }  


}
