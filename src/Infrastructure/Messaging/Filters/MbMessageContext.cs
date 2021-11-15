namespace YA.ServiceTemplate.Infrastructure.Messaging.Filters;

/// <summary>
/// Модель контекста исполнения сообщения шины данных
/// </summary>
internal class MbMessageContext
{
    internal Guid CorrelationId { get; set; }
}
