namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Dazinator.Extensions.DependencyInjection.ChildServiceProvider;
    using Microsoft.Extensions.DependencyInjection;
    using Xunit;

    public class ChildServiceProviderTests
    {

        [Theory]
        [InlineData(new[] { typeof(AnimalService) }, new Type[0])]
        [InlineData(new Type[0], new[] { typeof(AnimalService) })]
        [InlineData(new Type[0], new Type[0])]
        [InlineData(new[] { typeof(AnimalService) }, new[] { typeof(LionService) })]
        public void Can_Get_Parent_And_Child_Services(Type[] parentServices, Type[] childServices)
        {
            var parentServiceCollection = new ServiceCollection();
            foreach (var parentType in parentServices)
            {
                parentServiceCollection.AddTransient(parentType);
            }

            var sut = new ChildServiceCollection(parentServiceCollection.ToImmutableList());
            foreach (var childType in childServices)
            {
                sut.AddTransient(childType);
            }

            var parentServiceProvider = parentServiceCollection.BuildServiceProvider();
            var childServiceProvider = sut.BuildChildServiceProvider(parentServiceProvider);

            foreach (var item in parentServices)
            {
                var instance = childServiceProvider.GetRequiredService(item);
                Assert.NotNull(instance);
            }

            foreach (var item in childServices)
            {
                var instance = childServiceProvider.GetRequiredService(item);
                Assert.NotNull(instance);
            }
        }

    }
    public class AnimalService
    {
        public string SomeProperty { get; set; }
    }

    public class LionService : AnimalService
    {
        public bool SomeOtherProperty { get; set; }
    }
}
