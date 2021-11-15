using MassTransit;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Messages.Test;

public class TestRequestConsumer : IConsumer<IServiceTemplateTestRequestV1>
{
    public TestRequestConsumer()
    {

    }

    public async Task Consume(ConsumeContext<IServiceTemplateTestRequestV1> context)
    {
        await context.RespondAsync<IServiceTemplateTestResponseV1>(new
        {
            GotIt = context.Message.Timestamp
        });
    }
}
