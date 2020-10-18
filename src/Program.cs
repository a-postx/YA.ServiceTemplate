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
using Serilog;
using Serilog.Core;
using Serilog.Events;
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

namespace YA.ServiceTemplate
{
    enum OsPlatforms
    {
        Unknown = 0,
        Windows = 1,
        Linux = 2,
        OSX = 4
    }

    public static class Program
    {
        internal static readonly string AppName = Assembly.GetEntryAssembly()?.GetName().Name;
        internal static readonly Version AppVersion = Assembly.GetEntryAssembly()?.GetName().Version;
        internal static readonly string RootPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);

        internal static Countries Country { get; private set; }
        internal static OsPlatforms OsPlatform { get; private set; }

        public static async Task<int> Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Activity.DefaultIdFormat = ActivityIdFormat.W3C;

            OsPlatform = GetOs();

            IHostBuilder builder = CreateHostBuilder(args);

            IHost host;

            try
            {
                Console.WriteLine("Building Host...");

                host = builder.Build();

                Console.WriteLine("Host built successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building Host: {ex}.");
                return 1;
            }

            try
            {
                Log.Logger = CreateLogger(host);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error building logger: {ex}.");
                return 1;
            }

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

            try
            {
                await host.RunAsync();
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
                .UseSerilog()
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
            Console.WriteLine("Hosting environment is " + hostingEnvironment.EnvironmentName);

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

        private static Logger CreateLogger(IHost host)
        {
            IHostEnvironment hostEnv = host.Services.GetRequiredService<IHostEnvironment>();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            AppSecrets secrets = host.Services.GetRequiredService<IOptions<AppSecrets>>().Value;

            LoggerConfiguration loggerConfig = new LoggerConfiguration();

            loggerConfig
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("AppName", AppName)
                .Enrich.WithProperty("Version", AppVersion.ToString())
                .Enrich.WithProperty("NodeId", Node.Id)
                .Enrich.WithProperty("ProcessId", Process.GetCurrentProcess().Id)
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
                loggerConfig.WriteTo.Logzio(secrets.LogzioToken, 10, TimeSpan.FromSeconds(10), null, LogEventLevel.Debug);
            }

            Logger logger = loggerConfig.CreateLogger();

            if (string.IsNullOrEmpty(secrets.AppInsightsInstrumentationKey) && string.IsNullOrEmpty(secrets.LogzioToken))
            {
                logger.Warning("Sending logs to remote log managment systems is disabled.");
            }
            
            return logger;
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

        private static OsPlatforms GetOs()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OsPlatforms.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OsPlatforms.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OsPlatforms.OSX;
            }
            else
            {
                return OsPlatforms.Unknown;
            }
        }
    }
}