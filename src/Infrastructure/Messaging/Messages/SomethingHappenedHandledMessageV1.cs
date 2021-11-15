using MbEvents;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Messages;

internal class SomethingHappenedHandledMessageV1 : ISomethingHappenedHandledV1
{
    internal SomethingHappenedHandledMessageV1(Guid correlationId, string value)
    {
        CorrelationId = correlationId;
        Value = value;
    }

    public Guid CorrelationId { get; private set; }
    public string Value { get; private set; }
}
