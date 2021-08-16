using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YA.ServiceTemplate.Extensions;

namespace YA.ServiceTemplate.Health.System
{
    /// <summary>
    /// Checks network-related values of the application.
    /// </summary>
    public class NetworkHealthCheck : IHealthCheck
    { 
        public NetworkHealthCheck(ILogger<NetworkHealthCheck> logger, IOptionsMonitor<NetworkCheckOptions> options)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        private readonly ILogger<NetworkHealthCheck> _log;
        private readonly IOptionsMonitor<NetworkCheckOptions> _options;

        public string Name => "network_check";

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            NetworkCheckOptions opts = _options.Get(context.Registration.Name);

            bool networkIsAvailable = false;

            IPAddress ipAddress = IPAddress.Parse(opts.InternetHost);

            DateTime startDt = DateTime.UtcNow;

            try
            {
                networkIsAvailable = await ipAddress.CheckPingAsync();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error checking health for Network");
            }

            DateTime stopDt = DateTime.UtcNow;
            TimeSpan processingTime = stopDt - startDt;

            int networkCheckTime = (int)processingTime.TotalMilliseconds;

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "InternetConnectionLatency", networkCheckTime }
            };

            HealthStatus status = networkCheckTime < opts.MaxLatencyThreshold && networkIsAvailable ? HealthStatus.Healthy : HealthStatus.Unhealthy;

            return new HealthCheckResult(status, $"Reports degraded status if no Internet connection available or latency >= {opts.MaxLatencyThreshold} milliseconds.", null, data);
        }
    }
}
