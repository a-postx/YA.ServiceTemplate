using MassTransit;
using MbEvents;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages;

namespace YA.ServiceTemplate.Infrastructure.Messaging;

public class MessageBus : IMessageBus
{
    public MessageBus(ILogger<MessageBus> logger,
        IRuntimeContextAccessor runtimeContextAccessor,
        IPublishEndpoint publishEndpoint)
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _runtimeCtx = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    private readonly ILogger<MessageBus> _log;
    private readonly IRuntimeContextAccessor _runtimeCtx;
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task SomethingHappenedHandledV1Async(string value, CancellationToken cancellationToken)
    {
        await _publishEndpoint
            .Publish<ISomethingHappenedHandledV1>(new SomethingHappenedHandledMessageV1(_runtimeCtx.GetCorrelationId(), value), cancellationToken);
    }
}
