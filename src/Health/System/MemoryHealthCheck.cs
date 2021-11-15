using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace YA.ServiceTemplate.Health.System;

/// <summary>
/// Checks memory-related values of the application.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    public MemoryHealthCheck(IOptionsMonitor<MemoryCheckOptions> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    private readonly IOptionsMonitor<MemoryCheckOptions> _options;

    public string Name => "memory_check";

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        MemoryCheckOptions opts = _options.Get(context.Registration.Name);

        double workingSetMb = Process.GetCurrentProcess().WorkingSet64 / 1000000d;
        double privateMemoryMb = Process.GetCurrentProcess().PrivateMemorySize64 / 1000000d;
        double managedMemoryMb = GC.GetTotalMemory(forceFullCollection: false) / 1000000d;

        Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "WorkingSetMBytes", workingSetMb },
                { "PrivateMemoryMBytes", privateMemoryMb },
                { "ManagedMemoryMBytes", managedMemoryMb },
                { "Gen0Collections", GC.CollectionCount(0) },
                { "Gen1Collections", GC.CollectionCount(1) },
                { "Gen2Collections", GC.CollectionCount(2) }
            };

        HealthStatus status = managedMemoryMb < opts.ProcessMaxMemoryThreshold
            ? HealthStatus.Healthy
            : HealthStatus.Unhealthy;

        return Task.FromResult(new HealthCheckResult(status,
            $"Reports degraded status if managed memory size >= {opts.ProcessMaxMemoryThreshold} megabytes.", null, data));
    }
}
