using MassTransit;
using MbMessages;
using System;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Infrastructure.Messaging.Messages;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Consumers
{
    public class DoSomethingConsumer : IConsumer<IDoSomethingMessageV1>
    {
        public DoSomethingConsumer(IDoSomethingMessageHandler doSomethingMessageHandler)
        {
            _doSomethingMessageHandler = doSomethingMessageHandler ?? throw new ArgumentNullException(nameof(doSomethingMessageHandler));
        }

        private readonly IDoSomethingMessageHandler _doSomethingMessageHandler;

        public async Task Consume(ConsumeContext<IDoSomethingMessageV1> context)
        {
            //execute app command or other logic

            await _doSomethingMessageHandler.ServiceTheThingAsync(context.Message.Value);

            await context.RespondAsync<ISomethingDoneMessageV1>(new SomethingDoneMessageV1(context.Message.CorrelationId, $"Received: {context.Message.Value}. Answer: World!"));
        }
    }
}
