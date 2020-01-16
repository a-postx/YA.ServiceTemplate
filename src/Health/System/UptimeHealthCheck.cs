using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YA.ServiceTemplate.Health.System
{
    /// <summary>
    /// Checks uptime value of the application.
    /// </summary>
    public class UptimeHealthCheck : IHealthCheck
    {
        public string Name
        {
            get
            {
                return "uptime_check";
            }
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            TimeSpan runtime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            int upTimeValue = (runtime.Days * 3600) + (runtime.Minutes * 60) + runtime.Seconds;

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Uptime", upTimeValue }
            };

            HealthStatus status = HealthStatus.Healthy;

            return Task.FromResult(new HealthCheckResult(status, description: "Uptime, seconds", exception: null, data: data));
        }
    }
}
