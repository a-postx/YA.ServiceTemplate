using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Logging.Requests
{
    /// <summary>
    /// Прослойка логирования HTTP-контекста - запросов, ответов и исключений.
    /// В боевой среде функционал должен быть урезан из соображений безопасности.
    /// </summary>
    public class HttpContextLogger
    {
        public HttpContextLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext httpContext, IHostEnvironment env, IHostApplicationLifetime lifetime, IRuntimeContextAccessor runtimeCtx)
        {
            HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

            using (LogContext.PushProperty(Logs.LogType, LogTypes.ApiRequest.ToString()))
            {
                httpContext.Request.EnableBuffering();
                Stream body = httpContext.Request.Body;
                byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength, CultureInfo.InvariantCulture)];
                await httpContext.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), lifetime.ApplicationStopping);
                string initialRequestBody = Encoding.UTF8.GetString(buffer);
                body.Seek(0, SeekOrigin.Begin);
                httpContext.Request.Body = body;

                //logz.io/logstash fields can accept only 32k strings so request/response bodies are cut
                if (initialRequestBody.Length > General.MaxLogFieldLength)
                {
                    initialRequestBody = initialRequestBody.Substring(0, General.MaxLogFieldLength);
                }

                //у МС нет автоматического деструктурирования, поэтому используем Серилог ценой дырки в абстрации
                Log.ForContext(Logs.RequestHeaders, context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                    .ForContext(Logs.RequestBody, initialRequestBody)
                    .ForContext(Logs.RequestProtocol, context.Request.Protocol)
                    .ForContext(Logs.RequestScheme, context.Request.Scheme)
                    .ForContext(Logs.RequestHost, context.Request.Host.Value)
                    .ForContext(Logs.RequestMethod, context.Request.Method)
                    .ForContext(Logs.RequestPath, context.Request.Path)
                    .ForContext(Logs.RequestQuery, context.Request.QueryString)
                    .ForContext(Logs.RequestPathAndQuery, GetFullPath(context))
                    .Information("{RequestMethod} {RequestPath}", context.Request.Method, context.Request.Path);

                using (MemoryStream responseBodyMemoryStream = new MemoryStream())
                {
                    Stream originalResponseBodyReference = context.Response.Body;
                    context.Response.Body = responseBodyMemoryStream;

                    long start = Stopwatch.GetTimestamp();

                    try
                    {
                        await _next(context);
                    }
                    catch (Exception ex)
                    {
                        string errorMessage = ex.Message;
                        Log.Error(ex.Demystify(), "{ErrorMessage}", errorMessage);

                        ProblemDetails unknownError = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                            Status = StatusCodes.Status500InternalServerError,
                            Instance = context.Request.HttpContext.Request.Path,
                            Title = errorMessage,
                            Detail = env.IsDevelopment() ? ex.Demystify().StackTrace : null
                        };
                        unknownError.Extensions.Add("correlationId", runtimeCtx.GetCorrelationId().ToString());
                        unknownError.Extensions.Add("traceId", runtimeCtx.GetTraceId().ToString());

                        string errorResponseBody = JsonConvert.SerializeObject(unknownError);
                        context.Response.ContentType = "application/problem+json";
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                        await context.Response.WriteAsync(errorResponseBody, lifetime.ApplicationStopping);
                    }

                    double elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                    context.Response.Body.Seek(0, SeekOrigin.Begin);

                    string responseBody;

                    using (StreamReader sr = new StreamReader(context.Response.Body))
                    {
                        responseBody = await sr.ReadToEndAsync();

                        context.Response.Body.Seek(0, SeekOrigin.Begin);

                        string endResponseBody = (responseBody.Length > General.MaxLogFieldLength) ?
                            responseBody.Substring(0, General.MaxLogFieldLength) : responseBody;

                        Log.ForContext(Logs.ResponseHeaders, context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                            .ForContext(Logs.StatusCode, context.Response.StatusCode)
                            .ForContext(Logs.ResponseBody, endResponseBody)
                            .ForContext(Logs.ElapsedMilliseconds, elapsedMs)
                            .ForContext(Logs.RequestProtocol, context.Request.Protocol)
                            .ForContext(Logs.RequestScheme, context.Request.Scheme)
                            .ForContext(Logs.RequestHost, context.Request.Host.Value)
                            .ForContext(Logs.RequestMethod, context.Request.Method)
                            .ForContext(Logs.RequestPath, context.Request.Path)
                            .ForContext(Logs.RequestQuery, context.Request.QueryString)
                            .ForContext(Logs.RequestPathAndQuery, GetFullPath(context))
                            .Information("{RequestMethod} {RequestPath} - {StatusCode} in {ElapsedMilliseconds} ms", context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsedMs);

                        await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference, lifetime.ApplicationStopping);
                    }
                }
            }
        }

        private static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }

        private static string GetFullPath(HttpContext httpContext)
        {
            /*
                In some cases, like when running integration tests with WebApplicationFactory<T>
                the RawTarget returns an empty string instead of null, in that case we can't use
                ?? as fallback.
            */
            string requestPath = httpContext.Features.Get<IHttpRequestFeature>()?.RawTarget;

            if (string.IsNullOrEmpty(requestPath))
            {
                requestPath = httpContext.Request.Path.ToString();
            }

            return requestPath;
        }
    }
}
