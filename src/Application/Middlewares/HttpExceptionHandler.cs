using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Middlewares
{
    /// <summary>
    /// Прослойка перехвата исключения в HTTP-контексте. Выводит в дополнение к коду детали проблемы. 
    /// </summary>
    public class HttpExceptionHandler
    {
        public HttpExceptionHandler(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext context,
            IProblemDetailsFactory detailsFactory,
            IHostApplicationLifetime lifetime)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException ex)
            {
                if (!context.RequestAborted.IsCancellationRequested)
                {
                    await WriteProblemDetails(context, detailsFactory, lifetime, ex);
                }
            }
            catch (Exception ex)
            {
                await WriteProblemDetails(context, detailsFactory, lifetime, ex);
            }
        }

        private static async Task WriteProblemDetails(HttpContext context, IProblemDetailsFactory detailsFactory, IHostApplicationLifetime lifetime, Exception ex)
        {
            ProblemDetails problemDetails = detailsFactory.CreateProblemDetails(context, StatusCodes.Status500InternalServerError,
                                ex.Message, null, ex.Demystify().StackTrace, context.Request.HttpContext.Request.Path);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails, null, lifetime.ApplicationStopping);
        }
    }
}
