using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Delobytes.AspNetCore;
using Delobytes.AspNetCore.Swagger;
using Delobytes.AspNetCore.Swagger.OperationFilters;
using Delobytes.AspNetCore.Swagger.SchemaFilters;
using YA.ServiceTemplate.Health;
using YA.ServiceTemplate.Health.System;
using YA.ServiceTemplate.Health.Services;
using YA.ServiceTemplate.OperationFilters;
using YA.ServiceTemplate.Options;
using Microsoft.OpenApi.Models;
using YA.ServiceTemplate.Constants;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using CorrelationId.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;

namespace YA.ServiceTemplate
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods which extend ASP.NET Core services.
    /// </summary>
    internal static class CustomServiceCollectionExtensions
    {
        public static IServiceCollection AddCorrelationIdFluent(this IServiceCollection services)
        {
            services.AddDefaultCorrelationId(options =>
            {
                options.CorrelationIdGenerator = () => "";
                options.AddToLoggingScope = true;
                options.LoggingScopeKey = Logs.CorrelationId;
                options.EnforceHeader = false;
                options.IgnoreRequestHeader = false;
                options.IncludeInResponse = false;
                options.RequestHeader = General.CorrelationIdHeader;
                options.ResponseHeader = General.CorrelationIdHeader;
                options.UpdateTraceIdentifier = false;
            });

            return services;
        }

        /// <summary>
        /// Configures caching for the application. Registers the <see cref="IDistributedCache"/> and
        /// <see cref="IMemoryCache"/> types with the services collection or IoC container. The
        /// <see cref="IDistributedCache"/> is intended to be used in cloud hosted scenarios where there is a shared
        /// cache, which is shared between multiple instances of the application. Use the <see cref="IMemoryCache"/>
        /// otherwise.
        /// </summary>
        public static IServiceCollection AddCustomCaching(this IServiceCollection services)
        {
            return services
                .AddMemoryCache()
                .AddDistributedMemoryCache();
        }

        /// <summary>
        /// Add cross-origin resource sharing (CORS) services and configures named CORS policies. See
        /// https://docs.asp.net/en/latest/security/cors.html
        /// </summary>
        public static IServiceCollection AddCustomCors(this IServiceCollection services)
        {
            return services.AddCors(options =>
            {
                // Create named CORS policies here which you can consume using application.UseCors("PolicyName")
                // or a [EnableCors("PolicyName")] attribute on your controller or action.
                options.AddPolicy(
                    CorsPolicyName.AllowAny,
                    x => x
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });
        }

        /// <summary>
        /// Configures the settings by binding the contents of the appsettings.json file to the specified Plain Old CLR
        /// Objects (POCO) and adding <see cref="IOptions{T}"/> objects to the services collection.
        /// </summary>
        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                // ConfigureSingleton registers IOptions<T> and also T as a singleton to the services collection.
                .ConfigureAndValidateSingleton<ApplicationOptions>(configuration, o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<CompressionOptions>(configuration.GetSection(nameof(ApplicationOptions.Compression)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<ForwardedHeadersOptions>(configuration.GetSection(nameof(ApplicationOptions.ForwardedHeaders)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<CacheProfileOptions>(configuration.GetSection(nameof(ApplicationOptions.CacheProfiles)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<KestrelServerOptions>(configuration.GetSection(nameof(ApplicationOptions.Kestrel)), o => o.BindNonPublicProperties = false);
        }

        /// <summary>
        /// Adds dynamic response compression to enable GZIP compression of responses. This is turned off for HTTPS
        /// requests by default to avoid the BREACH security vulnerability.
        /// </summary>
        public static IServiceCollection AddCustomResponseCompression(this IServiceCollection services, IConfiguration configuration)
        {
            return services
                .Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
                .Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal)
                .AddResponseCompression(options =>
                {
                    // Add additional MIME types (other than the built in defaults) to enable GZIP compression for.
                    IEnumerable<string> customMimeTypes = configuration
                        .GetSection(nameof(ApplicationOptions.Compression))
                        .Get<CompressionOptions>()
                        ?.MimeTypes ?? Enumerable.Empty<string>();
                    options.MimeTypes = customMimeTypes.Concat(ResponseCompressionDefaults.MimeTypes);

                    options.Providers.Add<BrotliCompressionProvider>();
                    options.Providers.Add<GzipCompressionProvider>();
                });
        }

        /// <summary>
        /// Add custom routing settings which determines how URL's are generated.
        /// </summary>
        public static IServiceCollection AddCustomRouting(this IServiceCollection services)
        {
            return services.AddRouting(options =>
                {
                    options.LowercaseUrls = true;
                });
        }

        public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services)
        {
            services
                .AddSingleton<StartupServiceHealthCheck>()
                .AddSingleton<MessageBusServiceHealthCheck>()                
                .AddHealthChecks()
                    //general system status
                    .AddGenericHealthCheck<UptimeHealthCheck>("uptime")
                    .AddMemoryHealthCheck("memory")
                    //system components regular checks
                    .AddGenericHealthCheck<StartupServiceHealthCheck>("hosted_startup_service", HealthStatus.Degraded, new[] { "ready" })
                    .AddGenericHealthCheck<MessageBusServiceHealthCheck>(General.MessageBusServiceHealthCheckName, HealthStatus.Degraded, new[] { "ready" });
                    // Ping is not available on Azure Web Apps
                    //.AddNetworkHealthCheck("network");

            services.Configure<HealthCheckPublisherOptions>(options =>
            {
                options.Period = TimeSpan.FromSeconds(60);
                options.Timeout = TimeSpan.FromSeconds(60);
                options.Delay = TimeSpan.FromSeconds(15);
                options.Predicate = (check) => check.Tags.Contains("ready");
            });

            services.AddSingleton<IHealthCheckPublisher, ReadinessPublisher>();
            
            return services.AddHealthChecks().Services;
        }

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            return services.AddApiVersioning(options =>
                {
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                })
                .AddVersionedApiExplorer(x => x.GroupNameFormat = "'v'VVV"); // Version format: 'v'major[.minor][-status];
        }

        /// <summary>
        /// Add and configure Swagger services.
        /// </summary>
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
        {
            return services.AddSwaggerGen(options =>
                {
                    Assembly assembly = typeof(Startup).Assembly;
                    string assemblyProduct = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
                    string assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

                    options.DescribeAllParametersInCamelCase();
                    options.EnableAnnotations();
                    options.AddFluentValidationRules();

                    // Add the XML comment file for this assembly, so its contents can be displayed.
                    options.IncludeXmlCommentsIfExists(assembly);

                    options.OperationFilter<ApiVersionOperationFilter>();
                    options.OperationFilter<CorrelationIdOperationFilter>();
                    options.OperationFilter<ContentTypeOperationFilter>();

                    // Show an example model for JsonPatchDocument<T>.
                    options.SchemaFilter<JsonPatchDocumentSchemaFilter>();

                    IApiVersionDescriptionProvider provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                    foreach (ApiVersionDescription apiVersionDescription in provider.ApiVersionDescriptions)
                    {
                        OpenApiInfo info = new OpenApiInfo()
                        {
                            Title = assemblyProduct,
                            Description = apiVersionDescription.IsDeprecated
                                ? $"{assemblyDescription} This API version has been deprecated."
                                : assemblyDescription,
                            Version = apiVersionDescription.ApiVersion.ToString(),
                        };
                        options.SwaggerDoc(apiVersionDescription.GroupName, info);
                    }
                });
        }
    }
}
