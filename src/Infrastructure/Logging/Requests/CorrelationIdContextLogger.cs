using CorrelationId;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Logging.Requests
{
    /// <summary>
    /// CorrelationID context logging middleware. 
    /// </summary>
    public class CorrelationIdContextLogger
    {
        public CorrelationIdContextLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext httpContext, ILogger<CorrelationIdContextLogger> logger, ICorrelationContextAccessor correlationContextAccessor)
        {
            HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

            if (Guid.TryParse(correlationContextAccessor.CorrelationContext.CorrelationId, out Guid correlationId))
            {
                using (logger.BeginScopeWith((Logs.CorrelationId, correlationId)))
                {
                    await _next(context);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}
