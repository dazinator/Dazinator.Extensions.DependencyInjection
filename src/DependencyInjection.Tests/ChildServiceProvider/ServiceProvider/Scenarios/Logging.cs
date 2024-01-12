namespace DependencyInjection.Tests.ChildServiceProvider.ServiceProvider.Scenarios
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;
    using Dazinator.Extensions.DependencyInjection;
    using DependencyInjection.Tests.Utils;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Configuration;
    using Microsoft.Extensions.Options;
    using Xunit;
    using Xunit.Abstractions;
    using ServiceCollection = Dazinator.Extensions.DependencyInjection.ServiceCollection;

    // The following two parent level "singleton open generic" registrations are problematic
    // as they can't be resolved to the same parent instance from a child container - the child container will get its own
    // singletons.
    // I don't think that matters for ILogger<T> but let's find out.
    //  ServiceType: Microsoft.Extensions.Logging.ILogger`1
    //  ServiceType: Microsoft.Extensions.Logging.Configuration.ILoggerProviderConfiguration`1

    public class LoggingScenarioTests
    {

        public LoggingScenarioTests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [Theory()]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons)]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)]
        public void Can_Use_Parent_LoggerFactory_ILoggerT(ParentSingletonOpenGenericRegistrationsBehaviour behaviour)
        {
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole()
                       .AddXUnit(OutputHelper, (options) =>
                       {
                           options.IncludeScopes = true;
                       });
            });
            var serviceProvider = services.BuildServiceProvider();

            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
            {


            },
            s => s.BuildServiceProvider(),
            behaviour);

            //   https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/Microsoft.Extensions.Logging.Configuration/src/LoggerProviderConfiguration.cs#L10
            var childLogger = childServiceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();
            var parentLogger = serviceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();

            childLogger.LogInformation("Hello from child");
            parentLogger.LogInformation("Hello from parent");

            using (var childScope = childLogger.BeginScope("child scope"))
            {
                childLogger.LogInformation("within child scope");
            }

            using (var parentScope = parentLogger.BeginScope("parent scope"))
            {
                parentLogger.LogInformation("within parent scope");
            }


            // can we nest?
            using (var parentScope = parentLogger.BeginScope("parent scope wrap"))
            {
                parentLogger.LogInformation("hi from parent");

                using (var childScope = childLogger.BeginScope("child scope within"))
                {

                    childLogger.LogInformation("hi from child");
                }
            }

        }


        [Theory()]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons)]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)]
        public void Can_Use_Diffent_LoggerFactory_InChild(ParentSingletonOpenGenericRegistrationsBehaviour behaviour)
        {
            var services = new ServiceCollection();

            //https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/Microsoft.Extensions.Logging/src/LoggingServiceCollectionExtensions.cs
            services.AddLogging(builder =>
            {
                builder.AddXUnit(OutputHelper, (options) =>
                {
                    options.IncludeScopes = true;
                });
            });
            var serviceProvider = services.BuildServiceProvider();

            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
            {
                //  To configure logging differently in child container we should ovveride the singleton LoggerFactory from parent with a new isntance
                // By using LoggerFactory.Create we get to create an isolated instance
                var childLoggerFatory = LoggerFactory.Create((builder) =>
                {
                    // to allow logging in this child container to inherit condiguration from the root..
                    //var parentLoggingConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfigurationFactory>();
                    //builder.AddConfiguration(()

                    //  builder.Services.AddSingleton(typeof(ILoggerProviderConfiguration<>)));
                    builder.ClearProviders();
                    builder.AddXUnit(OutputHelper, (options) =>
                    {
                        options.IncludeScopes = true;
                    });
                    builder.SetMinimumLevel(LogLevel.Warning);
                });

                childServices.AddSingleton(childLoggerFatory);
            },
            s => s.BuildServiceProvider(),
            behaviour);

            var childLogger = childServiceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();
            var parentLogger = serviceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();


            childLogger.LogInformation("Hello from child you shouldn't see this");
            parentLogger.LogInformation("Hello from parent");


            using (var childScope = childLogger.BeginScope("child scope"))
            {
                childLogger.LogInformation("within child scope you won't see this as log level is warning for child");
                childLogger.LogWarning("within child scope");
            }

            using (var parentScope = parentLogger.BeginScope("parent scope"))
            {
                parentLogger.LogInformation("within parent scope");
            }

            // can we nest?
            using (var parentScope = parentLogger.BeginScope("parent scope wrap"))
            {
                parentLogger.LogInformation("hi from parent");

                using (var childScope = childLogger.BeginScope("child scope within"))
                {

                    childLogger.LogInformation("hi from child you wont see this");
                    childLogger.LogWarning("hi from child");
                }
            }
        }

        [Theory()]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.DuplicateSingletons)]
        [InlineData(ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)]
        [Description("In a parent container we should be able to have some singleton provider, which is written to based on log level filter x." +
            "In child container we should be able to also use that same provider instance (i.e for console provider this might be important) but it is only written to using a different log level filter configured for child container scope")]
        public void Can_Inherit_ParentLoggerProvider_InChild(ParentSingletonOpenGenericRegistrationsBehaviour behaviour)
        {
            var services = new ServiceCollection();

            var testLogSink = new TestSink();
            var loggerProvider = new TestLoggerProvider(testLogSink);

            // https://github.com/dotnet/runtime/blob/6072e4d3a7a2a1493f514cdf4be75a3d56580e84/src/libraries/Microsoft.Extensions.Logging/src/LoggingServiceCollectionExtensions.cs
            services.AddLogging(builder =>
            {
                builder.AddProvider(loggerProvider);
                builder.AddXUnit(OutputHelper, (options) =>
                {
                    options.IncludeScopes = true;
                });
            });
            var serviceProvider = services.BuildServiceProvider();

            var childServiceProvider = services.CreateChildServiceProvider(serviceProvider, (childServices) =>
            {
                //  To configure logging differently in child container we should ovveride the singleton LoggerFactory from parent with a new isntance

                // this doesn't work, because the parent is carrying an ILoggerFactory and ClearProviders() doesn't clear that.
                // childServices.AddLogging(a =>
                // {
                //     //  serviceProvider.GetRequiredService<ILoggerProvider>()
                //     //  builder.Services.AddSingleton(typeof(ILoggerProviderConfiguration<>)));
                //     a.ClearProviders(); // we may have inherited parent level providers here, clearproviders() will not clear those.
                //
                //     a.AddProvider(loggerProvider); // this provider is singleton instance we want to inherit from parent.
                //
                //     a.AddXUnit(OutputHelper, (options) =>
                //     {
                //         options.IncludeScopes = true;
                //     });
                //     a.SetMinimumLevel(LogLevel.Warning);
                //
                // });

                // this is necessary because AddLogging() calls TryAdd() - so we hide parent level registrations temporarily so that TryAdd() doesn't see them, and re-adds the same registrations at child level.

                // TODO - THIS IS BEING DISCARDED, WE NEED A NEW MORE POWERFUL API FOR THIS.
                // CONSIDER A BUILDER API THAT LETS YOOU CHAIN EXCLUDES AND INCLUDES.

                childServices = childServices.AutoPromoteChildDuplicates(a => a.IsSingleton(), (nested) =>
                {
                    nested.AddLogging(a =>
                    {
                        //  serviceProvider.GetRequiredService<ILoggerProvider>()
                        //  builder.Services.AddSingleton(typeof(ILoggerProviderConfiguration<>)));
                        a.ClearProviders(); // we may have inherited parent level providers here, clearproviders() will not clear those.

                        a.AddProvider(loggerProvider); // this provider is singleton instance we want to inherit from parent.

                        a.AddXUnit(OutputHelper, (options) =>
                        {
                            options.IncludeScopes = true;
                        });
                        a.SetMinimumLevel(LogLevel.Warning);

                    });

                });

               // childServices.AddLogging()
                 // By using LoggerFactory.Create we get to create an isolated instance
                //  var childLoggerFatory = LoggerFactory.Create((builder) =>
                //  {
                //      // to allow logging in this child container to inherit condiguration from the root..
                //      //var parentLoggingConfiguration = serviceProvider.GetRequiredService<ILoggerProviderConfigurationFactory>();
                //      //builder.AddConfiguration(()
                //
                //      //  serviceProvider.GetRequiredService<ILoggerProvider>()
                //      //  builder.Services.AddSingleton(typeof(ILoggerProviderConfiguration<>)));
                //      builder.ClearProviders();
                //
                //      builder.AddProvider(loggerProvider); // this provider is singleton instance we want to inherit from parent.
                //
                //      builder.AddXUnit(OutputHelper, (options) =>
                //      {
                //          options.IncludeScopes = true;
                //      });
                //      builder.SetMinimumLevel(LogLevel.Warning);
                //  });
                //
                // childServices.AddSingleton(childLoggerFatory);

                // we add these to stop them being delegate to the parent.
                // if (behaviour == ParentSingletonOpenGenericRegistrationsBehaviour.Delegate)
                // {
                //     childServices.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(Logger<>)));
                //
                //     childServices.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
                //         new DefaultLoggerLevelConfigureOptions(LogLevel.Information)));
                // }



            },
             s => s.BuildServiceProvider(),
             behaviour,
             allowModifyingParentServiceCollection: true); // this lets the child container modify the parent service collection, which means that "RemoveAll" will remove services inherited by default from the parent.

            var childLogger = childServiceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();
            var parentLogger = serviceProvider.GetRequiredService<ILogger<LoggingScenarioTests>>();

            childLogger.LogInformation("Hello from child you shouldn't see this");
            var writes = testLogSink.Writes;
            Assert.Empty(writes);

            parentLogger.LogInformation("Hello from parent");
            Assert.Single(writes);

            using (var childScope = childLogger.BeginScope("child scope"))
            {
                childLogger.LogInformation("within child scope you won't see this as log level is warning for child");
                Assert.Single(writes);

                childLogger.LogWarning("within child scope");
                Assert.Equal(2, writes.Count);
            }

            using (var parentScope = parentLogger.BeginScope("parent scope"))
            {
                parentLogger.LogInformation("within parent scope");
                Assert.Equal(3, writes.Count);
            }

            parentLogger.LogDebug("hi from parent shouldn't see this");
            Assert.Equal(3, writes.Count);

            // can we nest?
            using (var parentScope = parentLogger.BeginScope("parent scope wrap"))
            {
                parentLogger.LogInformation("hi from parent");
                Assert.Equal(4, writes.Count);


                using (var childScope = childLogger.BeginScope("child scope within"))
                {
                    childLogger.LogInformation("hi from child you wont see this");
                    Assert.Equal(4, writes.Count);

                    childLogger.LogWarning("hi from child");
                    Assert.Equal(5, writes.Count);
                }
            }
        }
    }
}
