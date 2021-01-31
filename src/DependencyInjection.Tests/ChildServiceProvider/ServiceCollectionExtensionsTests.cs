namespace Dazinator.Extensions.DependencyInjection.Tests.Child
{
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void CreateChildServiceCollection()
        {
            var parentServiceCollection = new ServiceCollection();
            var childServiceCollection = parentServiceCollection.CreateChildServiceCollection();
            Assert.NotNull(childServiceCollection);
        }

        [Fact]
        public void CreateChildServiceProvider()
        {
            IServiceCollection parentServiceCollection = new ServiceCollection();
            parentServiceCollection.AddTransient<ServiceCollectionExtensionsTests>();
            var parentServiceProvider = parentServiceCollection.BuildServiceProvider();

            var childServiceProvider = parentServiceProvider.CreateChildServiceProvider(parentServiceCollection, (childServices) =>
            {
                childServices.AddTransient<ServiceCollectionExtensionsTests>();
            }, sp => sp.BuildServiceProvider());
        }


    }
}
