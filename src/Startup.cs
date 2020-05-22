using System;
using System.Net.Http;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using VotRomania.Extensions;
using VotRomania.Options;
using VotRomania.Providers;
using VotRomania.Services;
using VotRomania.Stores;

namespace VotRomania
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _environment = environment;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddHealthChecks();
            services.Configure<DatabaseOptions>(Configuration.GetSection("Database"));

            services.AddDbContext<VotRomaniaContext>(ServiceLifetime.Singleton);
            services.AddSingleton<IDataProvider, DummyDataProvider>();
            services.AddSingleton<IPollingStationsRepository, PollingStationsRepository>();
            services.AddSingleton<IApplicationContentRepository, ApplicationContentRepository>();

            services.AddSingleton<IPollingStationSearchService, IneffectiveSearchService>();
            services.AddControllersWithViews();
            services.AddMediatR(Assembly.GetExecutingAssembly());

            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.DocumentFilter<OrderDefinitionsAlphabeticallyDocumentFilter>();
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "[Code4Ro] VotRomania API",
                    Contact = new OpenApiContact
                    {
                        Name = "Code4Romania Vot Romania",
                        Url = new Uri("https://github.com/code4romania/vot-romania")
                    }
                });
                options.SwaggerGeneratorOptions.DescribeAllParametersInCamelCase = true;
            });
            services.AddProblemDetails(ConfigureProblemDetails);
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        private void ConfigureProblemDetails(ProblemDetailsOptions options)
        {
            // This is the default behavior; only include exception details in a development environment.
            options.IncludeExceptionDetails = (ctx, ex) => _environment.IsDevelopment();

            // This will map NotImplementedException to the 501 Not Implemented status code.
            options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);

            // This will map HttpRequestException to the 503 Service Unavailable status code.
            options.MapToStatusCode<HttpRequestException>(StatusCodes.Status503ServiceUnavailable);

            // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
            // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
            options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
        }

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

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {

                options.DocumentTitle = "[Code4Ro] VotRomania API";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "[Code4Ro] VotRomania API v1");
            });

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });


            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
