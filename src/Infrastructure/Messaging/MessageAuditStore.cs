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
    /// <summary>
    /// Хранилище данных аудита сообщений шины данных, использующее простое логирование
    /// </summary>
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

            //корреляционный идентификатор перезаписывается, если уже существует
            using (_log.BeginScopeWith((Logs.LogType, LogTypes.MessageBusMessage.ToString()),
                (Logs.MessageBusContextType, metadata.ContextType),
                (Logs.MessageBusSourceAddress, metadata.SourceAddress),
                (Logs.MessageBusDestinationAddress, metadata.DestinationAddress),
                (Logs.MessageBusMessageId, metadata.MessageId),
                (Logs.CorrelationId, metadata.CorrelationId),
                (Logs.MessageBusConversationId, metadata.ConversationId),
                (Logs.MessageBusMessage, savedMessage)))
            {
                _log.LogInformation("{MessageBusContextType} message bus message {MessageBusMessageId}", metadata.ContextType, metadata.MessageId);
            }

            return Task.CompletedTask;
        }
    }
}
