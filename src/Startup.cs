using System;
using System.Text;
using Amazon.Extensions.NETCore.Setup;
using CorrelationId;
using Delobytes.AspNetCore;
using MediatR;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Middlewares.ResourceFilters;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Extensions;
using YA.ServiceTemplate.Health;
using YA.ServiceTemplate.Infrastructure.Caching;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate
{
    /// <summary>
    /// The main start-up class for the application.
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // controller design generator search for this
        private IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration, where key value pair settings are stored. See
        /// http://docs.asp.net/en/latest/fundamentals/configuration.html</param>
        /// <param name="webHostEnvironment">The environment the application is running under. This can be Development,
        /// Staging or Production by default. See http://docs.asp.net/en/latest/fundamentals/environments.html</param>
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));

            Configuration = configuration;
        }

        /// <summary>
        /// Configures the services to add to the ASP.NET Core Injection of Control (IoC) container. This method gets called by the ASP.NET runtime. See
        /// http://blogs.msdn.com/b/webdev/archive/2014/06/17/dependency-injection-in-asp-net-vnext.aspx
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddCustomOptions(_config)
                .AddOptionsAndSecretsValidationOnStartup();

            AppSecrets secrets = _config.GetSection(nameof(AppSecrets)).Get<AppSecrets>();
            GeneralOptions generalOptions = _config.GetSection(nameof(ApplicationOptions.General)).Get<GeneralOptions>();

            AWSOptions awsOptions = _config.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);

            if (!string.IsNullOrEmpty(secrets.AppInsightsInstrumentationKey))
            {
                ApplicationInsightsServiceOptions options = new ApplicationInsightsServiceOptions
                {
                    DeveloperMode = _webHostEnvironment.IsDevelopment(),
                    InstrumentationKey = secrets.AppInsightsInstrumentationKey
                };

                services.AddApplicationInsightsTelemetry(options);
            }

            services
                .AddCorrelationIdFluent(generalOptions)
                .AddCustomCaching()
                .AddCustomCors()
                .AddCustomRouting()
                .AddResponseCaching()
                .AddCustomResponseCompression(_config)
                .AddCustomHealthChecks()
                .AddCustomSwagger()
                .AddHttpContextAccessor()

                .AddSingleton<IActionContextAccessor, ActionContextAccessor>()

                .AddCustomApiVersioning();

            services
                .AddControllers()
                    .AddCustomJsonOptions(_webHostEnvironment)
                    .AddCustomMvcOptions(_config)
                    .AddCustomModelValidation();

            services.AddCustomProblemDetails();

            services.AddHttpClient();
            services.AddMediatR(GetType().Assembly);

            services
                .AddProjectActionHandlers()
                .AddProjectMappers()
                .AddProjectRepositories()
                .AddProjectServices();

            services.AddCustomMessageBus(secrets);

            services.AddScoped<IdempotencyFilterAttribute>();
            services.AddSingleton<IApiRequestMemoryCache, ApiRequestMemoryCache>();
        }

        /// <summary>
        /// Configures the application and HTTP request pipeline. Configure is called after ConfigureServices is called by the ASP.NET runtime.
        /// </summary>
        public void Configure(IApplicationBuilder application)
        {
            application
                .UseCorrelationId()

                .UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                })

                .UseResponseCaching()
                .UseResponseCompression()

                .UseNetworkContextLogging()
                .UseHttpContextLogging()
                .UseCustomExceptionHandler()

                .UseRouting()
                .UseCors(CorsPolicyName.AllowAny)
                .UseStaticFilesWithCacheControl()

                .UseIf(
                    _webHostEnvironment.IsDevelopment(),
                    x => x.UseDeveloperErrorPages())

                .UseHealthChecksPrometheusExporter("/metrics")
                .UseMetricServer()
                .UseHttpMetrics()

                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers().RequireCors(CorsPolicyName.AllowAny);
                    endpoints.MapHealthChecks("/status", new HealthCheckOptions()
                    {
                        ResponseWriter = HealthResponse.WriteResponseAsync
                    }).RequireCors(CorsPolicyName.AllowAny);
                    endpoints.MapHealthChecks("/status/ready", new HealthCheckOptions()
                    {
                        Predicate = (check) => check.Tags.Contains("ready"),
                        ResponseWriter = HealthResponse.WriteResponseAsync
                    }).RequireCors(CorsPolicyName.AllowAny);
                    endpoints.MapHealthChecks("/status/live", new HealthCheckOptions()
                    {
                        // Exclude all checks and return a 200-Ok.
                        Predicate = (_) => false,
                        ResponseWriter = HealthResponse.WriteResponseAsync
                    }).RequireCors(CorsPolicyName.AllowAny);
                    endpoints.MapGet("/nodeid", async (context) =>
                    {
                        await context.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(Node.Id));
                    }).RequireCors(CorsPolicyName.AllowAny);
                })

                .UseSwagger()
                .UseCustomSwaggerUI();
        }
    }
}
