using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace YA.ServiceTemplate.Health.Services
{
    /// <summary>
    /// Startup check for availability of external service (OData connector, http-service etc.).
    /// </summary>
    public class StartupServiceHealthCheck : IHealthCheck
    {
        public string Name
        {
            get
            {
                return "slow_dependency_check";
            }
        }

        public bool StartupTaskCompleted { get; set; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (StartupTaskCompleted)
            {
                return Task.FromResult(HealthCheckResult.Healthy("The startup task is finished."));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy("The startup task is still running."));
        }
    }
}
