namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Dazinator.AspNet.Extensions.FileProviders;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Xunit;
    
    public class OptionsScenarioTests
    {

        [Theory]
        [InlineData("")]
        public void Can_Use_Options_In_ChildContainer(params string[] args)
        {


            var services = new ServiceCollection();
            services.AddOptions();

            var serviceProvider = services.BuildServiceProvider();
            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
             {
                 childServices.Configure<TestOptions>(a =>
                 {
                     a.IsConfigured = true;
                 });
             },
             s => s.BuildServiceProvider(),
             ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons);


            var options = childServiceProvider.GetRequiredService<IOptions<TestOptions>>();
            Assert.True(options.Value.IsConfigured);


        }


        public class TestOptions
        {
            public bool IsConfigured { get; set; } = false;

        }
    }


}
