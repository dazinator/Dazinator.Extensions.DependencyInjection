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


    }
}
