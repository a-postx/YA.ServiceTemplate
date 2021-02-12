using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using YA.ServiceTemplate.Extensions;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Infrastructure.Logging.Requests
{
    /// <summary>
    /// Прослойка логирования контекста запроса с клиента. 
    /// </summary>
    public class ClientRequestContextLogger
    {
        public ClientRequestContextLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext httpContext,
            ILogger<ClientRequestContextLogger> logger,
            IOptions<IdempotencyControlOptions> idempotencyOptions)
        {
            HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

            if (context.Request.Headers
                    .TryGetValue(idempotencyOptions.Value.ClientRequestIdHeader, out StringValues clientRequestIdValue))
            {
                using (logger.BeginScopeWith(("ClientRequestId", clientRequestIdValue.First())))
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
