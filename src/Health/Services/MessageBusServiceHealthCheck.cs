using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages.Test;

namespace YA.ServiceTemplate.Health.Services;

/// <summary>
/// Regular check for availability of the message bus services.
/// </summary>
public class MessageBusServiceHealthCheck : IHealthCheck
{
    public MessageBusServiceHealthCheck(IBus bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    private readonly IBus _bus;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        Response<IServiceTemplateTestResponseV1> response = null;
        Dictionary<string, object> healthData = new Dictionary<string, object>();

        DateTime startDt = DateTime.UtcNow;

        try
        {
            response = await _bus
                .Request<IServiceTemplateTestRequestV1, IServiceTemplateTestResponseV1>(new { TimeStamp = now }, cancellationToken);
        }
        catch (RequestException ex)
        {
            healthData.Add("Exception", ex.Message);
        }
        finally
        {
            DateTime stopDt = DateTime.UtcNow;
            TimeSpan processingTime = stopDt - startDt;
            healthData.Add("Delay, msec", (int)processingTime.TotalMilliseconds);
        }

        return (response?.Message?.GotIt == now) ?
            HealthCheckResult.Healthy("Message Bus is available.", healthData) :
            HealthCheckResult.Unhealthy("Message Bus is unavailable.", null, healthData);
    }
}
