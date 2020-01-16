using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

            Stopwatch discoverySw = new Stopwatch();
            discoverySw.Start();            

            try
            {
                networkIsAvailable = await ipAddress.CheckPingAsync();
            }
            catch (Exception e)
            {
                _log.LogError("Error checking health for Network: {Exception}", e);
            }

            discoverySw.Stop();

            int networkLatencyValue = (int)discoverySw.ElapsedMilliseconds;

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "InternetConnectionLatency", networkLatencyValue }
            };

            HealthStatus status = networkLatencyValue < opts.MaxLatencyThreshold && networkIsAvailable ? HealthStatus.Healthy : HealthStatus.Unhealthy;

            return new HealthCheckResult(status, $"Reports degraded status if no Internet connection available or latency >= {opts.MaxLatencyThreshold} milliseconds.", null, data);
        }
    }
}
