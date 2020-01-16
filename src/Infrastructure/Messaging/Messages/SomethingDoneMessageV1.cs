using System;
using MbMessages;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Messages
{
    internal class SomethingDoneMessageV1 : ISomethingDoneMessageV1
    {
        internal SomethingDoneMessageV1(Guid correlationId, string value)
        {
            CorrelationId = correlationId;
            Value = value;
        }

        public Guid CorrelationId { get; private set; }
        public string Value { get; private set; }
    }
}
