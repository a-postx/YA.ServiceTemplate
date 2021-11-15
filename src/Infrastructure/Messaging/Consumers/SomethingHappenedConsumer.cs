using MassTransit;
using MbEvents;
using MediatR;
using YA.ServiceTemplate.Application.Features.SomeAggregate.Commands;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Consumers;

public class SomethingHappenedConsumer : IConsumer<ISomethingHappenedV1>
{
    public SomethingHappenedConsumer(IMediator mediator, IMessageBus messageBus)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
    }

    private readonly IMediator _mediator;
    private readonly IMessageBus _messageBus;

    public async Task Consume(ConsumeContext<ISomethingHappenedV1> context)
    {
        ICommandResult<string> result = await _mediator
            .Send(new DoSomethingCommand(context.Message.Value), context.CancellationToken);

        await _messageBus.SomethingHappenedHandledV1Async($"Received: {context.Message.Value}. Answer: {result.Data}", context.CancellationToken);
    }
}
