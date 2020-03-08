using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.Common.Utilities;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices.WindowsRuntime;
using Xunit;

namespace Dazinator.Extensions.DependencyInjection.Tests
{
    public class FuncNameResolverTests
    {
        [Fact]
        public void Can_Resolve_Singleton_From_Func_Name()
        {
            var services = new ServiceCollection();
            services.AddNamed<AnimalService>(names =>
            {
                names.AddSingleton<LionService>("A");
                names.AddSingleton<BearService>("B");
            });

            var sp = services.BuildServiceProvider();

            var funcResolver = sp.GetRequiredService<Func<string, AnimalService>>();

            var singletonInstanceA = funcResolver("A");
            Assert.IsType<LionService>(singletonInstanceA);

            var singletonInstanceB = funcResolver("B");
            Assert.IsType<BearService>(singletonInstanceB);

        }


    }
}
