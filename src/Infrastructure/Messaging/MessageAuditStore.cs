using System;
using System.Threading.Tasks;
using MassTransit.Audit;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Messaging
{
    public class MessageAuditStore : IMessageAuditStore
    {
        public MessageAuditStore(ILogger<MessageAuditStore> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private readonly ILogger<MessageAuditStore> _log;

        public Task StoreMessage<T>(T message, MessageAuditMetadata metadata) where T : class
        {
            string savedMessage = JToken.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented);

            //logz.io/logstash fields can accept only 32k strings so request/response bodies are cut
            if (savedMessage.Length > General.MaxLogFieldLength)
            {
                savedMessage = savedMessage.Substring(0, General.MaxLogFieldLength);
            }

            //CorrelationId being overwritten if exist
            _log.LogInformation("{LogType}{MessageBusContextType}{MessageBusDestinationAddress}{MessageBusSourceAddress}{CorrelationId}{MessageBusConversationId}{MessageBusMessage}",
                LogTypes.MessageBusMessage.ToString(), metadata.ContextType, metadata.DestinationAddress, metadata.SourceAddress,
                metadata.CorrelationId, metadata.ConversationId, savedMessage);

            return Task.CompletedTask;
        }
    }
}
