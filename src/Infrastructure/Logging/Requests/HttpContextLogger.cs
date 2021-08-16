using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Context;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Infrastructure.Logging.Requests
{
    /// <summary>
    /// Прослойка логирования HTTP-контекста - запросов и ответов.
    /// В боевой среде функционал должен быть урезан из соображений безопасности.
    /// </summary>
    public class HttpContextLogger
    {
        public HttpContextLogger(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        private readonly RequestDelegate _next;

        public async Task InvokeAsync(HttpContext httpContext,
            IHostApplicationLifetime lifetime,
            IOptions<GeneralOptions> options)
        {
            HttpContext context = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            int maxLogFieldLength = options.Value.MaxLogFieldLength;

            if (context.Request.Path == "/metrics")
            {
                using (LogContext.PushProperty(Logs.LogType, LogType.MetricRequest.ToString()))
                {
                    await _next(context);
                }
            }
            else
            {
                using (LogContext.PushProperty(Logs.LogType, LogType.ApiRequest.ToString()))
                {
                    httpContext.Request.EnableBuffering();
                    Stream body = httpContext.Request.Body;
                    byte[] buffer = new byte[Convert.ToInt32(httpContext.Request.ContentLength, CultureInfo.InvariantCulture)];
                    await httpContext.Request.Body.ReadAsync(buffer.AsMemory(0, buffer.Length), lifetime.ApplicationStopping);
                    string initialRequestBody = Encoding.UTF8.GetString(buffer);
                    body.Seek(0, SeekOrigin.Begin);
                    httpContext.Request.Body = body;

                    //logz.io/logstash fields can accept only 32k strings so request/response bodies are cut
                    if (initialRequestBody.Length > maxLogFieldLength)
                    {
                        initialRequestBody = initialRequestBody.Substring(0, maxLogFieldLength);
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

                        DateTime startDt = DateTime.UtcNow;

                        await _next(context);

                        DateTime stopDt = DateTime.UtcNow;
                        TimeSpan elapsedTimespan = stopDt - startDt;

                        context.Response.Body.Seek(0, SeekOrigin.Begin);

                        string responseBody;

                        using (StreamReader sr = new StreamReader(context.Response.Body))
                        {
                            responseBody = await sr.ReadToEndAsync();

                            context.Response.Body.Seek(0, SeekOrigin.Begin);

                            string endResponseBody = (responseBody.Length > maxLogFieldLength) ?
                                responseBody.Substring(0, maxLogFieldLength) : responseBody;

                            Log.ForContext(Logs.ResponseHeaders, context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()), true)
                                .ForContext(Logs.StatusCode, context.Response.StatusCode)
                                .ForContext(Logs.ResponseBody, endResponseBody)
                                .ForContext(Logs.ElapsedMilliseconds, (int)elapsedTimespan.TotalMilliseconds)
                                .ForContext(Logs.RequestProtocol, context.Request.Protocol)
                                .ForContext(Logs.RequestScheme, context.Request.Scheme)
                                .ForContext(Logs.RequestHost, context.Request.Host.Value)
                                .ForContext(Logs.RequestMethod, context.Request.Method)
                                .ForContext(Logs.RequestPath, context.Request.Path)
                                .ForContext(Logs.RequestQuery, context.Request.QueryString)
                                .ForContext(Logs.RequestPathAndQuery, GetFullPath(context))
                                .ForContext(Logs.RequestAborted, context.RequestAborted.IsCancellationRequested)
                                .Information("HTTP request handled.");

                            await responseBodyMemoryStream.CopyToAsync(originalResponseBodyReference, lifetime.ApplicationStopping);
                        }
                    }
                }
            }
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
