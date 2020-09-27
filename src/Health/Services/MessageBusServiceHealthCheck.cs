using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages.Test;

namespace YA.ServiceTemplate.Health.Services
{
    /// <summary>
    /// Regular check for availability of the message bus services.
    /// </summary>
    public class MessageBusServiceHealthCheck : IHealthCheck
    {
        public MessageBusServiceHealthCheck(ILogger<MessageBusServiceHealthCheck> logger, IBus bus)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        private readonly ILogger<MessageBusServiceHealthCheck> _log;
        private readonly IBus _bus;

        public string Name => General.MessageBusServiceHealthCheckName;

        public bool MessageBusStartupTaskCompleted { get; set; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;
            Response<IServiceTemplateTestResponseV1> response = null;
            Dictionary<string, object> healthData = new Dictionary<string, object>();

            if (MessageBusStartupTaskCompleted)
            {
                Stopwatch mbSw = new Stopwatch();
                mbSw.Start();

                try
                {
                    response = await _bus.Request<IServiceTemplateTestRequestV1, IServiceTemplateTestResponseV1>(new { TimeStamp = now }, cancellationToken);
                }
                catch (RequestException ex)
                {
                    healthData.Add("Exception", ex.Message);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error checking health for Message Bus");
                    healthData.Add("Exception", ex.Message);
                }
                finally
                {
                    mbSw.Stop();
                    healthData.Add("Delay, msec", mbSw.ElapsedMilliseconds);
                }

                return (response?.Message?.GotIt == now) ?
                    HealthCheckResult.Healthy("Message Bus is available.", healthData) : 
                    HealthCheckResult.Unhealthy("Message Bus is unavailable.", null, healthData);
            }
            else
            {
                return HealthCheckResult.Unhealthy("Message Bus is unavailable.", null, healthData);
            }
        }
    }
}
