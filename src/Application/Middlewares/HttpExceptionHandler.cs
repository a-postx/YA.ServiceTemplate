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
            catch (Exception ex)
            {
                ProblemDetails unknownError = detailsFactory.CreateProblemDetails(context, StatusCodes.Status500InternalServerError,
                    ex.Message, null, ex.Demystify().StackTrace, context.Request.HttpContext.Request.Path);

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await JsonSerializer.SerializeAsync(context.Response.Body, unknownError, null, lifetime.ApplicationStopping);
            }
        }
    }
}
