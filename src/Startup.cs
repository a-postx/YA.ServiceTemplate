using Amazon.Extensions.NETCore.Setup;
using CorrelationId;
using Delobytes.AspNetCore;
using GreenPipes;
using MassTransit;
using MassTransit.Audit;
using MassTransit.RabbitMqTransport;
using MbMessages;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using YA.ServiceTemplate.Application;
using YA.ServiceTemplate.Application.ActionFilters;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Health;
using YA.ServiceTemplate.Infrastructure.Caching;
using YA.ServiceTemplate.Infrastructure.Messaging;
using YA.ServiceTemplate.Infrastructure.Messaging.Consumers;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages.Test;

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
            AWSOptions awsOptions = _config.GetAWSOptions();
            services.AddDefaultAWSOptions(awsOptions);

            AppSecrets secrets = _config.Get<AppSecrets>();

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
                .AddCorrelationIdFluent()

                .AddCustomCaching()
                .AddCustomCors()
                .AddCustomOptions(_config)
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
                    .AddCustomMvcOptions(_config);

            services
                .AddProjectCommands()
                .AddProjectMappers()
                .AddProjectRepositories()
                .AddProjectServices();

            services.AddScoped<TestRequestConsumer>();
            services.AddScoped<IDoSomethingMessageHandler, DoSomethingMessageHandler>();
            services.AddScoped<DoSomethingConsumer>();

            services.AddMassTransit(options =>
            {
                options.AddConsumers(GetType().Assembly);
            });

            services.AddSingleton(provider =>
            {
                return Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    IRabbitMqHost host = cfg.Host(secrets.MessageBusHost, secrets.MessageBusVHost, h =>
                    {
                        h.Username(secrets.MessageBusLogin);
                        h.Password(secrets.MessageBusPassword);
                    });

                    cfg.UseSerilog();
                    cfg.UseSerilogMessagePropertiesEnricher();

                    cfg.ReceiveEndpoint(host, MbQueueNames.PrivateServiceQueueName, e =>
                    {
                        e.PrefetchCount = 16;
                        e.UseMessageRetry(x => x.Interval(2, 500));
                        e.AutoDelete = true;
                        e.Durable = false;
                        e.ExchangeType = "fanout";
                        e.Exclusive = true;
                        e.ExclusiveConsumer = true;

                        e.Consumer<TestRequestConsumer>(provider);
                    });

                    cfg.ReceiveEndpoint(host, "ya.servicetemplate.receiveendpoint", e =>
                    {
                        e.PrefetchCount = 16;
                        e.UseMessageRetry(x => x.Interval(2, 100));

                        e.Consumer<DoSomethingConsumer>(provider);
                    });
                });
            });

            services.AddSingleton<IPublishEndpoint>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<ISendEndpointProvider>(provider => provider.GetRequiredService<IBusControl>());
            services.AddSingleton<IBus>(provider => provider.GetRequiredService<IBusControl>());

            services.AddScoped(provider => provider.GetRequiredService<IBus>().CreateRequestClient<IDoSomethingMessageV1>());

            services.AddSingleton<IMessageAuditStore, MessageAuditStore>();

            services.AddScoped<ApiRequestFilter>();
            services.AddScoped<IApiRequestTracker, ApiRequestTracker>();
            services.AddSingleton<IApiRequestMemoryCache, ApiRequestMemoryCache>();
        }

        /// <summary>
        /// Configures the application and HTTP request pipeline. Configure is called after ConfigureServices is called by the ASP.NET runtime.
        /// </summary>
        public void Configure(IApplicationBuilder application)
        {
            application
                .UseCorrelationId(new CorrelationIdOptions
                {
                    Header = General.CorrelationIdHeader,
                    IncludeInResponse = false,
                    UpdateTraceIdentifier = true,
                    UseGuidForCorrelationId = false
                })
                .UseCorrelationIdContextLogging()

                .UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                })

                .UseResponseCaching()
                .UseResponseCompression()

                .UseHttpContextLogging()
                
                .UseRouting()
                .UseCors(CorsPolicyName.AllowAny)
                .UseStaticFilesWithCacheControl()

                .UseIf(
                    _webHostEnvironment.IsDevelopment(),
                    x => x.UseDeveloperErrorPages())

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
                })

                .UseSwagger()
                .UseCustomSwaggerUI();
        }
    }
}