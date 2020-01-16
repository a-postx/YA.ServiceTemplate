using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YA.ServiceTemplate.Health.Services;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    /// <summary>
    /// Slow starting hosted service.
    /// </summary>
    public class StartupService : BackgroundService
    {
        public StartupService(ILogger<StartupService> logger, StartupServiceHealthCheck startupHostedServiceHealthCheck)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _startupHostedServiceHealthCheck = startupHostedServiceHealthCheck;
        }

        private readonly ILogger<StartupService> _log;
        private readonly StartupServiceHealthCheck _startupHostedServiceHealthCheck;
        private readonly int _delaySeconds = 15;

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation(nameof(StartupService) + " background service is starting...");

            Task startupServiceTask = Task.Run(async () =>
            {
                await Task.Delay(_delaySeconds * 1000, cancellationToken);

                _startupHostedServiceHealthCheck.StartupTaskCompleted = true;
                _log.LogInformation(nameof(StartupService) + " background service has started.");
            });

            return Task.CompletedTask;
        }

        //не выполняется, сервис находится как IHostedService
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _log.LogInformation(nameof(StartupService) + " background service is stopping...");
            _log.LogInformation(nameof(StartupService) + " background service gracefully stopped.");

            return Task.CompletedTask;
        }
    }
}
