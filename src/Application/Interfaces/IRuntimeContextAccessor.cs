namespace YA.ServiceTemplate.Application.Interfaces;

public interface IRuntimeContextAccessor
{
    Guid GetCorrelationId();
    string GetTraceId();
}
