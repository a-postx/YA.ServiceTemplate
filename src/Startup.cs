using System.Text;
using Amazon.Extensions.NETCore.Setup;
using CorrelationId;
using Delobytes.AspNetCore;
using Delobytes.AspNetCore.Idempotency;
using Delobytes.AspNetCore.Logging;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Extensions;
using YA.ServiceTemplate.Health;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate;

/// <summary>
/// The main start-up class for the application.
/// </summary>
public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));

        Configuration = configuration;
    }

    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _webHostEnvironment;

    // controller design generator search for this
    private IConfiguration Configuration { get; }

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
        IdempotencyOptions idempotencyOptions = _config
            .GetSection(nameof(ApplicationOptions.IdempotencyControl)).Get<IdempotencyOptions>();

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
            .AddCustomSwagger(idempotencyOptions)
            .AddFluentValidationRulesToSwagger()
            .AddHttpContextAccessor()

            .AddSingleton<IActionContextAccessor, ActionContextAccessor>()

            .AddCustomApiVersioning();

        services.AddIdempotencyContextLogging(options =>
        {
            options.IdempotencyLogAttribute = "IdempotencyKey";
        });

        services.AddHttpContextLogging(options =>
        {
            options.LogRequestBody = true;
            options.LogResponseBody = true;
            options.MaxBodyLength = generalOptions.MaxLogFieldLength;
            options.SkipPaths = new List<PathString> { "/metrics" };
        });

        services
            .AddControllers()
                .AddCustomJsonOptions(_webHostEnvironment)
                .AddCustomMvcOptions(_config)
                .AddCustomModelValidation();

        services.AddIdempotencyControl(options =>
        {
            options.Enabled = idempotencyOptions.IdempotencyFilterEnabled ?? false;
            options.HeaderRequired = true;
            options.IdempotencyHeader = idempotencyOptions.IdempotencyHeader;
        });

        services.AddHttpClient();
        services.AddMediatR(GetType().Assembly);

        services
            .AddProjectActionHandlers()
            .AddProjectMappers()
            .AddProjectRepositories()
            .AddProjectServices();

        services.AddCustomMessageBus(secrets);
    }

    /// <summary>
    /// Configures the application and HTTP request pipeline. Configure is called after ConfigureServices is called by the ASP.NET runtime.
    /// </summary>
    public void Configure(IApplicationBuilder application)
    {
        IdempotencyControlOptions idempotencyOptions = _config
            .GetSection(nameof(ApplicationOptions.IdempotencyControl)).Get<IdempotencyControlOptions>();

        if (idempotencyOptions.Enabled)
        {
            application.UseIdempotencyContextLogging();
        }

        application
            .UseCorrelationId()

            .UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
            })
            .UseNetworkLogging()

            .UseHttpExceptionHandling(options => options.IncludeStackTraceInResponse = true)

            .UseRouting()
            .UseCors(CorsPolicyName.AllowAny)
            .UseResponseCaching()
            .UseResponseCompression()
            .UseHttpContextLogging()
            .UseStaticFilesWithCacheControl()

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
