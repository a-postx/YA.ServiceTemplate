using MassTransit;
using MbMessages;
using MediatR;
using System;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Features.SomeAggregate.Commands;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Consumers
{
    public class DoSomethingConsumer : IConsumer<IDoSomethingMessageV1>
    {
        public DoSomethingConsumer(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        private readonly IMediator _mediator;

        public async Task Consume(ConsumeContext<IDoSomethingMessageV1> context)
        {
            //execute app command or other logic
            ICommandResult<string> result = await _mediator
                .Send(new DoSomethingCommand(context.Message.Value), context.CancellationToken);

            await context.RespondAsync<ISomethingDoneMessageV1>(new SomethingDoneMessageV1(context.Message.CorrelationId, $"Received: {context.Message.Value}. Answer: {result.Data}"));
        }
    }
}
