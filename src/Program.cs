using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Delobytes.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Prometheus.DotNetRuntime;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Sinks.Logz.Io;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Options;

[assembly: CLSCompliant(false)]
namespace YA.ServiceTemplate
{
    internal enum OsPlatform
    {
        Unknown = 0,
        Windows = 1,
        Linux = 2,
        OSX = 3
    }

    public static class Program
    {
        internal static readonly string AppName = Assembly.GetEntryAssembly()?.GetName().Name;
        internal static readonly Version AppVersion = Assembly.GetEntryAssembly()?.GetName().Version;
        internal static readonly string RootPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

        internal static Countries Country { get; private set; }
        internal static OsPlatform OsPlatform { get; private set; }

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            Log.Logger = CreateBootstrapLogger();

            IDisposable dotNetRuntimeStats = null;

            try
            {
                Log.Information("Building Host...");

                OsPlatform = GetOs();

                IHost host = CreateHostBuilder(args).Build();

                Log.Information("Host built successfully.");

                IHostEnvironment hostEnvironment = host.Services.GetRequiredService<IHostEnvironment>();
                Log.Information("Hosting environment is {EnvironmentName}", hostEnvironment.EnvironmentName);

                string coreCLR = ((AssemblyInformationalVersionAttribute[])typeof(object).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion;
                string coreFX = ((AssemblyInformationalVersionAttribute[])typeof(Uri).Assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false))[0].InformationalVersion;

                Log.Information("Application.Name: {AppName}\n Application.Version: {AppVersion}\n " +
                    "Environment.Version: {EnvVersion}\n RuntimeInformation.FrameworkDescription: {RuntimeInfo}\n " +
                    "CoreCLR Build: {CoreClrBuild}\n CoreCLR Hash: {CoreClrHash}\n " +
                    "CoreFX Build: {CoreFxBuild}\n CoreFX Hash: {CoreFxHash}\n " +
                    "Environment.OSVersion {OsVersion}\n RuntimeInformation.OSDescription: {OsDescr}\n " +
                    "RuntimeInformation.OSArchitecture: {OsArch}\n Environment.ProcessorCount: {CpuCount}",
                    AppName, AppVersion, Environment.Version, RuntimeInformation.FrameworkDescription, coreCLR.Split('+')[0],
                    coreCLR.Split('+')[1], coreFX.Split('+')[0], coreFX.Split('+')[1], Environment.OSVersion,
                    RuntimeInformation.OSDescription, RuntimeInformation.OSArchitecture, Environment.ProcessorCount);

                IRuntimeGeoDataService geoService = host.Services.GetService<IRuntimeGeoDataService>();
                using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    Country = await geoService.GetCountryCodeAsync(cts.Token);
                }

                IHostApplicationLifetime hostLifetime = host.Services.GetService<IHostApplicationLifetime>();
                hostLifetime.ApplicationStopping.Register(() =>
                {
                    host.Services.GetRequiredService<ILogger<Startup>>().LogInformation("Shutdown has been initiated.");
                });

                IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
                //вызывается дважды при изменениях на файловой системе https://github.com/dotnet/aspnetcore/issues/2542
                ChangeToken.OnChange(configuration.GetReloadToken, () =>
                {
                    host.Services.GetRequiredService<ILogger<Startup>>().LogInformation("Options or secrets has been modified.");
                });

                dotNetRuntimeStats = DotNetRuntimeStatsBuilder.Default().StartCollecting();

                await host.RunAsync().ConfigureAwait(false);

                Log.Information("{AppName} has stopped.", AppName);
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "{AppName} terminated unexpectedly.", AppName);
                return 1;
            }
            finally
            {
                dotNetRuntimeStats?.Dispose();
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = new HostBuilder().UseContentRoot(Directory.GetCurrentDirectory());
            hostBuilder
                .ConfigureHostConfiguration(
                    configurationBuilder => configurationBuilder
                        .AddEnvironmentVariables(prefix: "DOTNET_")
                        .AddIf(
                            args != null,
                            x => x.AddCommandLine(args)))
                .ConfigureAppConfiguration((hostingContext, config) =>
                    AddConfiguration(config, hostingContext.HostingEnvironment, args))
                .UseSerilog(ConfigureReloadableLogger)
                .UseDefaultServiceProvider(
                    (context, options) =>
                    {
                        bool isDevelopment = context.HostingEnvironment.IsDevelopment();
                        options.ValidateScopes = isDevelopment;
                        options.ValidateOnBuild = isDevelopment;
                    })
                .ConfigureWebHost(ConfigureWebHostBuilder);

            return hostBuilder;
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder webHostBuilder)
        {
            webHostBuilder
                .UseKestrel(
                    (builderContext, options) =>
                    {
                        options.AddServerHeader = false;

                        options.Configure(builderContext.Configuration.GetSection(nameof(ApplicationOptions.Kestrel)));
                        ConfigureKestrelServerLimits(builderContext, options);
                    })
                //<--AzureAppServicesIntegration
                .UseAzureAppServices()
                .UseSetting("detailedErrors", "true")
                .CaptureStartupErrors(true)
                //AzureAppServicesIntegration--/>

                // Used for IIS and IIS Express for in-process hosting. Use UseIISIntegration for out-of-process hosting.
                .UseIIS()
                .UseShutdownTimeout(TimeSpan.FromSeconds(Timeouts.WebHostShutdownTimeoutSec))
                .UseStartup<Startup>();
        }

        private static IConfigurationBuilder AddConfiguration(IConfigurationBuilder configurationBuilder, IHostEnvironment hostingEnvironment, string[] args)
        {
            IConfigurationRoot tempConfig = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            AWSCredentials credentials = new BasicAWSCredentials(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID"), Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"));
            AWSOptions awsOptions = new AWSOptions()
            {
                Credentials = credentials,
                Region = RegionEndpoint.GetBySystemName(tempConfig.GetValue<string>("AWS:Region"))
            };

            configurationBuilder
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            string awsSharedParameterStorePath = $"/{hostingEnvironment.EnvironmentName.ToLowerInvariant()}";

            configurationBuilder.AddSystemsManager(config =>
            {
                config.AwsOptions = awsOptions;
                config.Optional = false;
                config.Path = awsSharedParameterStorePath;
                config.ReloadAfter = TimeSpan.FromDays(1);
                config.OnLoadException += exceptionContext =>
                {
                    //log
                };
            });

            // Добавляем параметры командной строки, которые имеют наивысший приоритет.
            configurationBuilder
                .AddIf(
                    args != null,
                    x => x.AddCommandLine(args));

            return configurationBuilder;
        }

        /// <summary>
        /// Creates a logger used during application initialization.
        /// <see href="https://nblumhardt.com/2020/10/bootstrap-logger/"/>.
        /// </summary>
        /// <returns>A logger that can load a new configuration.</returns>
        private static ReloadableLogger CreateBootstrapLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();
        }

        /// <summary>
        /// Добавляет расширенный логер с засылкой данных в удалённые системы
        /// </summary>
        private static void ConfigureReloadableLogger(HostBuilderContext context, IServiceProvider services, LoggerConfiguration loggerConfig)
        {
            IHostEnvironment hostEnv = services.GetRequiredService<IHostEnvironment>();
            IConfiguration configuration = services.GetRequiredService<IConfiguration>();
            AppSecrets secrets = services.GetRequiredService<IOptions<AppSecrets>>().Value;

            loggerConfig
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("AppName", AppName)
                .Enrich.WithProperty("Version", AppVersion.ToString())
                .Enrich.WithProperty("NodeId", Node.Id)
                .Enrich.WithProperty("ProcessId", Environment.ProcessId)
                .Enrich.WithProperty("ProcessName", Process.GetCurrentProcess().ProcessName)
                .Enrich.WithProperty("MachineName", Environment.MachineName)
                .Enrich.WithProperty("EnvironmentName", hostEnv.EnvironmentName)
                .Enrich.WithProperty("EnvironmentUserName", Environment.UserName)
                .Enrich.WithProperty("OSPlatform", OsPlatform.ToString())
                .Enrich.FromMassTransitMessage();

            if (!string.IsNullOrEmpty(secrets.AppInsightsInstrumentationKey))
            {
                loggerConfig.WriteTo.ApplicationInsights(secrets.AppInsightsInstrumentationKey, TelemetryConverter.Traces, LogEventLevel.Debug);
            }

            if (!string.IsNullOrEmpty(secrets.LogzioToken))
            {
                loggerConfig.WriteTo.LogzIo(secrets.LogzioToken, null,
                    new LogzioOptions
                    {
                        DataCenterSubDomain = "listener-eu",
                        UseHttps = false,
                        RestrictedToMinimumLevel = LogEventLevel.Debug,
                        Period = TimeSpan.FromSeconds(10),
                        BatchPostingLimit = 10
                    });
            }

            if (string.IsNullOrEmpty(secrets.AppInsightsInstrumentationKey) && string.IsNullOrEmpty(secrets.LogzioToken))
            {
                Log.Warning("Sending logs to remote log managment systems is disabled.");
            }
        }

        /// <summary>
        /// Configure Kestrel server limits from appsettings.json is not supported so we manually copy from config.
        /// https://github.com/aspnet/KestrelHttpServer/issues/2216
        /// </summary>
        private static void ConfigureKestrelServerLimits(WebHostBuilderContext builderContext, KestrelServerOptions options)
        {
            KestrelServerOptions source = new KestrelServerOptions();
            builderContext.Configuration.GetSection(nameof(ApplicationOptions.Kestrel)).Bind(source);

            KestrelServerLimits limits = options.Limits;
            KestrelServerLimits sourceLimits = source.Limits;

            Http2Limits http2 = limits.Http2;
            Http2Limits sourceHttp2 = sourceLimits.Http2;

            http2.HeaderTableSize = sourceHttp2.HeaderTableSize;
            http2.InitialConnectionWindowSize = sourceHttp2.InitialConnectionWindowSize;
            http2.InitialStreamWindowSize = sourceHttp2.InitialStreamWindowSize;
            http2.MaxFrameSize = sourceHttp2.MaxFrameSize;
            http2.MaxRequestHeaderFieldSize = sourceHttp2.MaxRequestHeaderFieldSize;
            http2.MaxStreamsPerConnection = sourceHttp2.MaxStreamsPerConnection;

            limits.KeepAliveTimeout = sourceLimits.KeepAliveTimeout;
            limits.MaxConcurrentConnections = sourceLimits.MaxConcurrentConnections;
            limits.MaxConcurrentUpgradedConnections = sourceLimits.MaxConcurrentUpgradedConnections;
            limits.MaxRequestBodySize = sourceLimits.MaxRequestBodySize;
            limits.MaxRequestBufferSize = sourceLimits.MaxRequestBufferSize;
            limits.MaxRequestHeaderCount = sourceLimits.MaxRequestHeaderCount;
            limits.MaxRequestHeadersTotalSize = sourceLimits.MaxRequestHeadersTotalSize;
            //https://github.com/aspnet/AspNetCore/issues/12614
            limits.MaxRequestLineSize = sourceLimits.MaxRequestLineSize - 10;
            limits.MaxResponseBufferSize = sourceLimits.MaxResponseBufferSize;
            limits.MinRequestBodyDataRate = sourceLimits.MinRequestBodyDataRate;
            limits.MinResponseDataRate = sourceLimits.MinResponseDataRate;
            limits.RequestHeadersTimeout = sourceLimits.RequestHeadersTimeout;
        }

        private static OsPlatform GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OsPlatform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OsPlatform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OsPlatform.OSX;
            }
            else
            {
                return OsPlatform.Unknown;
            }
        }
    }
}
