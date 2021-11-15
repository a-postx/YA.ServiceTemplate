using System.Diagnostics;
using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using YA.ServiceTemplate.Application.Exceptions;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Infrastructure.Messaging.Filters;

namespace YA.ServiceTemplate.Infrastructure.Services;

public class RuntimeContextAccessor : IRuntimeContextAccessor
{
    public RuntimeContextAccessor(ILogger<RuntimeContextAccessor> logger,
        IHttpContextAccessor httpContextAccessor,
        ICorrelationContextAccessor correlationContextAccessor)
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _correlationCtx = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
        _httpCtx = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private readonly ILogger<RuntimeContextAccessor> _log;
    private readonly IHttpContextAccessor _httpCtx;
    private readonly ICorrelationContextAccessor _correlationCtx;

    public Guid GetCorrelationId()
    {
        MbMessageContext mbMessageContext = MbMessageContextProvider.Current;

        if (_correlationCtx.CorrelationContext != null && mbMessageContext != null)
        {
            throw new CorrelationIdNotFoundException("Cannot obtain CorrelationID: both contexts are presented.");
        }

        if (_correlationCtx.CorrelationContext == null && mbMessageContext == null)
        {
            return Guid.Empty;
        }

        //веб-запрос
        if (_correlationCtx.CorrelationContext != null && mbMessageContext == null)
        {
            if (Guid.TryParse(_correlationCtx.CorrelationContext.CorrelationId, out Guid correlationId))
            {
                return correlationId;
            }
            else
            {
                return Guid.Empty;
            }
        }

        //запрос из шины
        if (_correlationCtx.CorrelationContext == null && mbMessageContext != null)
        {
            if (mbMessageContext.CorrelationId == Guid.Empty)
            {
                throw new CorrelationIdNotFoundException("Cannot obtain CorrelationID from message bus message.");
            }

            return mbMessageContext.CorrelationId;
        }

        throw new CorrelationIdNotFoundException("Cannot obtain CorrelationID: no context.");
    }

    public string GetTraceId()
    {
        string traceId = Activity.Current?.Id ?? _httpCtx.HttpContext.TraceIdentifier;
        return traceId;
    }
}
