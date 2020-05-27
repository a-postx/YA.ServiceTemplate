using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Logging.Requests
{
    /// <summary>
    /// HTTP request, response and exceptions logging middleware. 
    /// </summary>
    public class HttpContextLogger
    {
        public HttpContextLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext httpContext, IHostEnvironment env, ICorrelationContextAccessor correlationContextAccessor)
        {
            HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            
            LogContext.PushProperty(Logs.LogType, LogTypes.BackendApiRequest.ToString());

            httpContext.Request.EnableBuffering();
            Stream body = httpContext.Request.Body;
            byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength)];
            await httpContext.Request.Body.ReadAsync(buffer, 0, buffer.Length);
            string initialRequestBody = Encoding.UTF8.GetString(buffer);
            body.Seek(0, SeekOrigin.Begin);
            httpContext.Request.Body = body;

            //logz.io/logstash fields can accept only 32k strings so request/response bodies are cut
            if (initialRequestBody.Length > General.MaxLogFieldLength)
            {
                initialRequestBody = initialRequestBody.Substring(0, General.MaxLogFieldLength);
            }

            Log.ForContext(Logs.RequestHeaders, context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                .ForContext(Logs.RequestBody, initialRequestBody)
                .Information("Request information {RequestMethod} {RequestPath} information", context.Request.Method, context.Request.Path);

            using (MemoryStream responseBodyMemoryStream = new MemoryStream())
            {
                Stream originalResponseBodyReference = context.Response.Body;
                context.Response.Body = responseBodyMemoryStream;
                
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    string errorMessage = ex.Message;
                    Log.Error(ex.Demystify(), "{ErrorMessage}", errorMessage);

                    ApiProblemDetails unknownError;

                    if (env.IsDevelopment())
                    {
                        unknownError = new ApiProblemDetails("https://tools.ietf.org/html/rfc7231#section-6.6.1", StatusCodes.Status500InternalServerError,
                            context.Request.HttpContext.Request.Path, errorMessage, ex.Demystify().StackTrace, correlationContextAccessor.CorrelationContext.CorrelationId,
                            context.Request.HttpContext.TraceIdentifier);
                    }
                    else
                    {
                        unknownError = new ApiProblemDetails("https://tools.ietf.org/html/rfc7231#section-6.6.1", StatusCodes.Status500InternalServerError,
                            context.Request.HttpContext.Request.Path, errorMessage, null, correlationContextAccessor.CorrelationContext.CorrelationId,
                            context.Request.HttpContext.TraceIdentifier);
                    }

                    string errorResponseBody = JsonConvert.SerializeObject(unknownError);
                    context.Response.ContentType = "application/problem+json";
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    await context.Response.WriteAsync(errorResponseBody);
                }

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                if (Enumerable.Range(400, 599).Contains(context.Response.StatusCode) && responseBody.Contains("traceId", StringComparison.InvariantCultureIgnoreCase))
                {
                    ApiProblemDetails problem = JsonConvert.DeserializeObject<ApiProblemDetails>(responseBody);
                    LogContext.PushProperty(Logs.TraceId, problem.TraceId);
                }

                string endResponseBody = (responseBody.Length > General.MaxLogFieldLength) ?
                    responseBody.Substring(0, General.MaxLogFieldLength) : responseBody;

                Log.ForContext(Logs.ResponseHeaders, context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                    .ForContext(Logs.ResponseBody, endResponseBody)
                    .Information("Response information {RequestMethod} {RequestPath} {StatusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);

                await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference);
            }
        }
    }
}
