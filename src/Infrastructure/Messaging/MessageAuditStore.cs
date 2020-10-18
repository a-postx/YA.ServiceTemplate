using System;
using System.Threading.Tasks;
using MassTransit.Audit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Extensions;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Infrastructure.Messaging
{
    /// <summary>
    /// Хранилище данных аудита сообщений шины данных, использующее простое логирование
    /// </summary>
    public class MessageAuditStore : IMessageAuditStore
    {
        public MessageAuditStore(ILogger<MessageAuditStore> logger, IOptionsMonitor<GeneralOptions> optionsMonitor)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxLogFieldLength = optionsMonitor.CurrentValue.MaxLogFieldLength;
        }

        private readonly ILogger<MessageAuditStore> _log;
        private readonly int _maxLogFieldLength;

        public Task StoreMessage<T>(T message, MessageAuditMetadata metadata) where T : class
        {
            string savedMessage = JToken.Parse(JsonConvert.SerializeObject(message)).ToString(Formatting.Indented);

            //logz.io/logstash fields can accept only 32k strings so request/response bodies are cut
            if (savedMessage.Length > _maxLogFieldLength)
            {
                savedMessage = savedMessage.Substring(0, _maxLogFieldLength);
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
