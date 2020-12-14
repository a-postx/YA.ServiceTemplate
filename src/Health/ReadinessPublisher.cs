using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace YA.ServiceTemplate.Health
{
    /// <summary>
    /// Публикатор для регулярной засылки проверок здоровья в пуш-системы.
    /// </summary>
    public class ReadinessPublisher : IHealthCheckPublisher
    {
        public ReadinessPublisher(ILogger<ReadinessPublisher> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger<ReadinessPublisher> _log;

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            _log.LogInformation("{Timestamp} Readiness Probe Status: {Result} ({CheckDuration}).", DateTime.UtcNow, report.Status, report.TotalDuration);

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
}
