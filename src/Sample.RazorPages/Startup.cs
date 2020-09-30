using Dazinator.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dazinator.Extensions.DependencyInjection.ChildContainers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace Sample.RazorPages
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            Services = services;


            //services.AddRazorPages();
        }

        public IServiceProvider ChildContainer { get; set; } = null;
        public RequestDelegate ChildRequestDelegate { get; set; } = null;

        public IServiceCollection Services { get; set; }

        public IHostApplicationLifetime HostLifetime { get; set; } = null;

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            //  app.UseStaticFiles();

            //  app.UseRouting();

            //  app.UseAuthorization();

            app.MapWhen((a) => ChildContainer == null, builder =>
            {
                //var childServices = Services.CreateChildServiceCollection();
                var appServices = builder.ApplicationServices;

                HostLifetime = appServices.GetRequiredService<IHostApplicationLifetime>();

                ChildContainer = Services.CreateChildServiceCollection()
                                         .ConfigureServices(child=>child.AddOptions())
                                         .AutoPromoteChildDuplicates(d => d.IsSingletonOpenGeneric(),
                                                                  (child) => ConfigureChildServices(child, builder))
                                      .BuildChildServiceProvider(appServices);
            });

            app.Use(async (context, next) =>
            {
                // var old = context.RequestServices;
                var childScope = ChildContainer.CreateScope();
                context.Response.RegisterForDispose(childScope);
                //if (old is IDisposable oldDisposable)
                //{
                //    context.Response.RegisterForDispose(oldDisposable);
                //}
                context.RequestServices = childScope.ServiceProvider;
                await next();
            });


            var childPipeline = ChildContainer.GetRequiredService<RequestDelegate>();
            app.Use(async (httpContext, next) =>
            {
                await childPipeline(httpContext);
                await next();
            });

        }

        private void ConfigureChildServices(IChildServiceCollection services, IApplicationBuilder app)
        {
            services.AddOptions();
            services.AddLogging(builder => builder.AddConfiguration());

            services.AddSingleton<ChildOnlyService>();

            services.AddRazorPages((o) =>
            {
                o.RootDirectory = $"/Child";
            });

            services.AddSingleton<RequestDelegate>(sp =>
            {

                var childBuilder = app.New();
                childBuilder.ApplicationServices = ChildContainer;
                ConfigureChildPipeline(childBuilder);

                // register root pipeline at the end of the tenant branch
                //if (next != null && reJoin)
                //{
                //    childBuilder.Run(next);
                //}
                return childBuilder.Build();
            });
        }

        private void ConfigureChildPipeline(IApplicationBuilder branchBuilder)
        {
            // var middlewarePipline = new ApplicationBuilder(sp);

            branchBuilder.Use(async (httpContext, next) =>
            {
                //     await httpContext.Response.WriteAsync("Child pipeline fired");
                await next();
            });
            branchBuilder.UseRouting();
            branchBuilder.UseAuthorization();

            branchBuilder.Use(async (c, next) =>
            {
                // Demonstrates per tenant files.
                // /foo.txt exists for one tenant but not another.
                var opts = c.RequestServices.GetRequiredService<IOptions<RazorPagesOptions>>();


                var webHostEnvironment = c.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var contentFileProvider = webHostEnvironment.ContentRootFileProvider;
                var webFileProvider = webHostEnvironment.WebRootFileProvider;

                var fileExists = contentFileProvider.GetFileInfo("/ChildPages/Index.cshtml");
                Console.WriteLine($"file exists? {fileExists.Exists}");

                var tenantHostLifetime = ChildContainer.GetRequiredService<IHostApplicationLifetime>();
                var isSame = Object.ReferenceEquals(HostLifetime, tenantHostLifetime);

                await next.Invoke();
            });


            branchBuilder.UseEndpoints(endpoints =>
            {
                var sp = endpoints.ServiceProvider;
                var childOnly = sp.GetRequiredService<ChildOnlyService>();



                var options = sp.GetRequiredService<IOptions<RazorPagesOptions>>();
                var rootDir = options.Value.RootDirectory;

                // var provider = sp.GetRequiredService<PageActionDescriptorProvider>();

                //var childOptions = ChildContainer.GetRequiredService<IOptions<RazorPagesOptions>>();
                //var childRootDir = childOptions.Value.RootDirectory;


                // endpoints.CreateApplicationBuilder()

                bool isChild = object.Equals(sp, ChildContainer);
                endpoints.MapRazorPages();



            });
        }
    }

    public class ChildOnlyService
    {

    }

}
