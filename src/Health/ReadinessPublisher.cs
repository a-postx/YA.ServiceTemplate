using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace YA.ServiceTemplate.Health
{
    /// <summary>
    /// Regular check (30 sec) for availability of the required internal services.
    /// </summary>
    public class ReadinessPublisher : IHealthCheckPublisher
    {
        public ReadinessPublisher(ILogger<ReadinessPublisher> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger<ReadinessPublisher> _log;

        public List<(HealthReport report, CancellationToken cancellationToken)> Entries { get; } = new List<(HealthReport report, CancellationToken cancellationToken)>();
        public Exception Exception { get; set; }

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            Entries.Add((report, cancellationToken));

            _log.LogInformation("{TIMESTAMP} Readiness Probe Status: {RESULT}", DateTime.UtcNow, report.Status);

            if (Exception != null)
            {
                throw Exception;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.CompletedTask;
        }
    }
}
