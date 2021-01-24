namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class RazorPagesScenarioTests
    {

        [Theory]
        [InlineData("")]
        public void Can_Use_RazorPages_In_ChildContainer(params string[] args)
        {


            var services = new ServiceCollection();
            // services.AddOptions();


            var serviceProvider = services.BuildServiceProvider();
            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
             {
                 childServices.AddRazorPages(o =>
                 {
                     o.RootDirectory = "/Child";
                 });
             },
             s => s.BuildServiceProvider(),
             ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons);


            var options = childServiceProvider.GetRequiredService<IOptions<RazorPagesOptions>>();
            Assert.Equal("/Child", options.Value.RootDirectory);

        }


        public class TestOptions
        {
            public bool IsConfigured { get; set; } = false;

        }
    }


}
