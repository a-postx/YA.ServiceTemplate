using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Middlewares
{
    /// <summary>
    /// Прослойка перехвата исключения в HTTP-контексте и выводу вместо него кода 500 с ProblemDetails. 
    /// </summary>
    public class HttpExceptionHandler
    {
        public HttpExceptionHandler(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext context,
            IHostApplicationLifetime lifetime,
            IRuntimeContextAccessor runtimeCtx)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                string errorMessage = ex.Message;

                ProblemDetails unknownError = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = context.Request.HttpContext.Request.Path,
                    Title = errorMessage,
                    Detail = ex.Demystify().StackTrace
                };
                unknownError.Extensions.Add("correlationId", runtimeCtx.GetCorrelationId().ToString());
                unknownError.Extensions.Add("traceId", runtimeCtx.GetTraceId().ToString());

                string errorResponseBody = JsonConvert.SerializeObject(unknownError);
                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsync(errorResponseBody, lifetime.ApplicationStopping);
            }
        }
    }
}
