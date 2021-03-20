using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using CorrelationId.DependencyInjection;
using Delobytes.AspNetCore;
using GreenPipes;
using MassTransit;
using MassTransit.Audit;
using MassTransit.PrometheusIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Health;
using YA.ServiceTemplate.Health.Services;
using YA.ServiceTemplate.Health.System;
using YA.ServiceTemplate.Infrastructure.Messaging;
using YA.ServiceTemplate.Infrastructure.Messaging.Consumers;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages.Test;
using YA.ServiceTemplate.OpenApi;
using YA.ServiceTemplate.Options;
using YA.ServiceTemplate.Options.Validators;

namespace YA.ServiceTemplate.Extensions
{
    /// <summary>
    /// <see cref="IServiceCollection"/> extension methods which extend ASP.NET Core services.
    /// </summary>
    internal static class CustomServiceCollectionExtensions
    {
        public static IServiceCollection AddCorrelationIdFluent(this IServiceCollection services, GeneralOptions generalOptions)
        {
            services.AddDefaultCorrelationId(options =>
            {
                options.CorrelationIdGenerator = () => Guid.NewGuid().ToString();
                options.AddToLoggingScope = true;
                options.LoggingScopeKey = Logs.CorrelationId;
                options.EnforceHeader = false;
                options.IgnoreRequestHeader = true;
                options.IncludeInResponse = true;
                options.RequestHeader = generalOptions.CorrelationIdHeader;
                options.ResponseHeader = generalOptions.CorrelationIdHeader;
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
        /// Configures the settings and secrets by binding the contents of the files and remote sources
        /// to the specified POCO and adding <see cref="IOptions{T}"/> objects to the services collection.
        /// </summary>
        public static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IValidateOptions<HostOptions>, HostOptionsValidator>();
            services.AddSingleton<IValidateOptions<AwsOptions>, AwsOptionsValidator>();
            services.AddSingleton<IValidateOptions<GeneralOptions>, GeneralOptionsValidator>();
            services.AddSingleton<IValidateOptions<IdempotencyControlOptions>, IdempotencyControlOptionsValidator>();

            services.AddSingleton<IValidateOptions<AppSecrets>, AppSecretsValidator>();

            services
                .ConfigureAndValidateSingleton<ApplicationOptions>(configuration, o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<HostOptions>(configuration.GetSection(nameof(ApplicationOptions.HostOptions)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<AwsOptions>(configuration.GetSection(nameof(ApplicationOptions.Aws)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<CompressionOptions>(configuration.GetSection(nameof(ApplicationOptions.Compression)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<ForwardedHeadersOptions>(configuration.GetSection(nameof(ApplicationOptions.ForwardedHeaders)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<CacheProfileOptions>(configuration.GetSection(nameof(ApplicationOptions.CacheProfiles)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<KestrelServerOptions>(configuration.GetSection(nameof(ApplicationOptions.Kestrel)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<GeneralOptions>(configuration.GetSection(nameof(ApplicationOptions.General)), o => o.BindNonPublicProperties = false)
                .ConfigureAndValidateSingleton<IdempotencyControlOptions>(configuration.GetSection(nameof(ApplicationOptions.IdempotencyControl)), o => o.BindNonPublicProperties = false)

                .ConfigureAndValidateSingleton<AppSecrets>(configuration.GetSection(nameof(AppSecrets)), o => o.BindNonPublicProperties = false);

            return services;
        }

        /// <summary>
        /// Создаёт экземпляры всех настроек и получает значения, чтобы провести процесс валидации при старте приложения.
        /// </summary>
        public static IServiceCollection AddOptionsAndSecretsValidationOnStartup(this IServiceCollection services)
        {
            ////перместить валидацию в общий процесс прогрева https://andrewlock.net/reducing-latency-by-pre-building-singletons-in-asp-net-core/
            try
            {
                HostOptions hostOptions = services.BuildServiceProvider().GetService<IOptions<HostOptions>>().Value;
                AwsOptions awsOptions = services.BuildServiceProvider().GetService<IOptions<AwsOptions>>().Value;
                ApplicationOptions applicationOptions = services.BuildServiceProvider().GetService<IOptions<ApplicationOptions>>().Value;
                GeneralOptions generalOptions = services.BuildServiceProvider().GetService<IOptions<GeneralOptions>>().Value;
                IdempotencyControlOptions idempotencyOptions = services.BuildServiceProvider().GetService<IOptions<IdempotencyControlOptions>>().Value;

                AppSecrets appSecrets = services.BuildServiceProvider().GetService<IOptions<AppSecrets>>().Value;
            }
            catch (OptionsValidationException ex)
            {
                Console.WriteLine($"Error validating {ex.OptionsType.FullName}: {string.Join(", ", ex.Failures)}");
                throw;
            }

            return services;
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
                        .Get<CompressionOptions>()?.MimeTypes ?? Enumerable.Empty<string>();
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
                .AddSingleton<MessageBusServiceHealthCheck>()                
                .AddHealthChecks()
                    //general system status
                    .AddGenericHealthCheck<UptimeHealthCheck>("uptime")
                    .AddMemoryHealthCheck("memory")
                    //system components regular checks
                    .AddGenericHealthCheck<MessageBusServiceHealthCheck>("message_bus_service", HealthStatus.Degraded, new[] { "ready" });
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
            
            return services;
        }

        public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
        {
            return services.AddApiVersioning(options =>
                {
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                    options.ApiVersionReader = new QueryStringApiVersionReader("api-version");
                })
                .AddVersionedApiExplorer(x => x.GroupNameFormat = "'v'VVV"); // Version format: 'v'major[.minor][-status];
        }

        /// <summary>
        /// Add and configure Swagger services.
        /// </summary>
        public static IServiceCollection AddCustomSwagger(this IServiceCollection services, IdempotencyControlOptions idempotencyOptions)
        {
            services
                .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
                .AddSwaggerGen();

            return services;
        }

        /// <summary>
        /// Добавляет шину данных МассТранзит.
        /// </summary>
        public static IServiceCollection AddCustomMessageBus(this IServiceCollection services, AppSecrets secrets)
        {
            services.AddSingleton<IMessageAuditStore, MessageAuditStore>();

            services.AddMassTransit(options =>
            {
                options.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(secrets.MessageBusHost, secrets.MessageBusVHost, h =>
                    {
                        h.Username(secrets.MessageBusLogin);
                        h.Password(secrets.MessageBusPassword);
                    });

                    IMessageAuditStore auditStore = context.GetRequiredService<IMessageAuditStore>();
                    cfg.ConnectSendAuditObservers(auditStore, c => c.Exclude(typeof(IServiceTemplateTestRequestV1), typeof(IServiceTemplateTestResponseV1)));
                    cfg.ConnectConsumeAuditObserver(auditStore, c => c.Exclude(typeof(IServiceTemplateTestRequestV1), typeof(IServiceTemplateTestResponseV1)));

                    cfg.UseHealthCheck(context);

                    cfg.UseSerilogMessagePropertiesEnricher();
                    cfg.UsePrometheusMetrics();

                    cfg.ReceiveEndpoint(MbQueueNames.PrivateServiceQueueName, e =>
                    {
                        e.PrefetchCount = 16;
                        e.UseMessageRetry(x => x.Interval(2, 500));
                        e.AutoDelete = true;
                        e.Durable = false;
                        e.ExchangeType = "fanout";
                        e.Exclusive = true;
                        e.ExclusiveConsumer = true;

                        e.ConfigureConsumer<TestRequestConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("ya.servicetemplate.receiveendpoint", e =>
                    {
                        e.PrefetchCount = 16;
                        e.UseMessageRetry(x => x.Interval(2, 100));
                        e.UseMbContextFilter();

                        e.ConfigureConsumer<DoSomethingConsumer>(context);
                    });
                });

                options.AddConsumers(Assembly.GetExecutingAssembly());
            });

            services.AddMassTransitHostedService();

            services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());

            return services;
        }
    }
}
