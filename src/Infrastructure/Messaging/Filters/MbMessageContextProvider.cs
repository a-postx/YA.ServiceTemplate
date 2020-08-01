using GreenPipes;
using MassTransit;
using System;

namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters
{
    /// <summary>
    /// Provides context from message bus message.
    /// </summary>
    internal static class MbMessageContextProvider
    {
        public static MbMessageContext Current => GetData();

        private static MbMessageContext GetData()
        {
            MbMessageContext mbMessageContext = new MbMessageContext();

            PipeContext current = MbMessageContextStack.Current;

            ConsumeContext<CorrelatedBy<Guid>> correlationIdContext = current?.GetPayload<ConsumeContext<CorrelatedBy<Guid>>>();

            if (correlationIdContext != null)
            {
                mbMessageContext.CorrelationId = correlationIdContext.Message.CorrelationId;
            }

            if (mbMessageContext.CorrelationId != Guid.Empty)
            {
                return mbMessageContext;
            }
            else
            {
                return null;
            }
        }
    }
}
