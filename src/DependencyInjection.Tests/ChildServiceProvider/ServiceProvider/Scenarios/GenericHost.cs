namespace Dazinator.Extensions.DependencyInjection.Tests.ChildServiceProvider
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Xunit;

    public class GenericHostTests
    {

        #region Generic Host

        [Theory]
        [Description("Tests creating child containers with a generic host.")]
        [InlineData("")]
        public async Task Options_WorksInChildContainers(params string[] args)
        {
            IHost host = null;
            var cancelTokenSource = new CancellationTokenSource();

            var builder = new HostBuilder();
            builder.UseContentRoot(Directory.GetCurrentDirectory());

            builder.ConfigureHostConfiguration(config =>
            {
                config.AddEnvironmentVariables();
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            });

            builder.ConfigureLogging((hostingContext, logging) =>
              {
                  // logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                  logging.AddConsole();
                  logging.AddDebug();
                  logging.AddEventSourceLogger();
              })
               .ConfigureServices(s =>
                {
                    s.AddHostedService<TestHostedService>(sp =>
                    {
                        var hostLifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                        return new TestHostedService(sp, s, hostLifetime);
                    });
                })


            .UseDefaultServiceProvider((context, options) => options.ValidateScopes = context.HostingEnvironment.IsDevelopment());

            host = builder.Build();
            await host.StartAsync();
            //var host = await builder.StartAsync();


            // https://github.com/aspnet/Hosting/blob/master/src/Microsoft.Extensions.Hosting/HostBuilder.cs#L183

            var cancelToken = cancelTokenSource.Token;
            var runningTask = host.RunAsync(cancelToken);

            var runningSeconds = 0;
            while (!cancelToken.IsCancellationRequested && !runningTask.IsCompleted)
            {
                if (!runningTask.IsCompleted)
                {
                    Console.WriteLine("host running..");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    runningSeconds += 1;
                }
            }
        }

        #endregion


    }

    public class TestHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IServiceCollection _services;
        private readonly IHostApplicationLifetime _hostAppLifetime;

        public TestHostedService(IServiceProvider serviceProvider, IServiceCollection services, IHostApplicationLifetime hostAppLifetime)
        {
            _serviceProvider = serviceProvider;
            _services = services;
            _hostAppLifetime = hostAppLifetime;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            // run test logic

            // These aren't supported, if we don't do something about them, we'll get an exception.
            // var removed = _services.RemoveSingletonOpenGenerics();
            var childServices = _services.CreateChildServiceCollection(ParentSingletonOpenGenericRegistrationsBehaviour.Omit);
            childServices.AddLogging();

            var childServiceProvider = childServices.BuildChildServiceProvider(_serviceProvider);

            // verify that logging and options services work within child container.
            // These are fairly fundamental.
            var logger = childServiceProvider.GetRequiredService<ILogger<TestHostedService>>();
            var options = childServiceProvider.GetRequiredService<IOptions<TestOptions>>();
            Success = true;
            _hostAppLifetime.StopApplication();
            return Task.CompletedTask;

        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public bool Success { get; set; }
    }

    public class TestOptions
    {

    }

}
