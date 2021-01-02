using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Service;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Application.Middlewares.ResourceFilters
{
    /// <summary>
    /// Фильтр идемпотентности: не допускает запросов без идентификатора,
    /// сохраняет запрос и результат в кеш, чтобы вернуть тот же ответ в случае запроса-дубликата.
    /// Реализация по примеру https://stripe.com/docs/api/idempotent_requests
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IdempotencyFilterAttribute : Attribute, IAsyncResourceFilter
    {
        public IdempotencyFilterAttribute(ILogger<IdempotencyFilterAttribute> logger,
            IApiRequestMemoryCache cacheService,
            IRuntimeContextAccessor runtimeContextAccessor,
            IOptions<GeneralOptions> options,
            IProblemDetailsFactory problemDetailsFactory)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _runtimeCtx = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));
            _generalOptions = options.Value;
            _pdFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        }

        private readonly ILogger<IdempotencyFilterAttribute> _log;
        private readonly IApiRequestMemoryCache _cacheService;
        private readonly IRuntimeContextAccessor _runtimeCtx;
        private readonly GeneralOptions _generalOptions;
        private readonly IProblemDetailsFactory _pdFactory;

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            if (_generalOptions.IdempotencyFilterEnabled.HasValue && _generalOptions.IdempotencyFilterEnabled.Value)
            {
                Guid requestId = _runtimeCtx.GetCorrelationId();

                if (requestId == Guid.Empty)
                {
                    ProblemDetails problemDetails = _pdFactory.CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest,
                                $"Запрос не содержит заголовка {_generalOptions.CorrelationIdHeader} или значение в нём неверно.",
                                null, null, context.HttpContext.Request.Path);

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }

                string method = context.HttpContext.Request.Method;
                string path = context.HttpContext.Request.Path.HasValue ? context.HttpContext.Request.Path.Value : null;
                string query = context.HttpContext.Request.QueryString.HasValue ? context.HttpContext.Request.QueryString.ToUriComponent() : null;

                using (CancellationTokenSource cts = new CancellationTokenSource(Timeouts.ApiRequestFilterMs))
                {
                    (bool requestCreated, ApiRequest request) = GetOrCreateRequest(requestId, method, path, query);

                    if (!requestCreated)
                    {
                        if (method != request.Method || path != request.Path || query != request.Query)
                        {
                            ProblemDetails apiRequestParamError = _pdFactory.CreateProblemDetails(context.HttpContext, StatusCodes.Status409Conflict,
                            "В кеше исполнения уже есть запрос с таким идентификатором и его параметры отличны от текущего запроса.", null, null, context.HttpContext.Request.Path);

                            context.Result = new BadRequestObjectResult(apiRequestParamError);
                            return;
                        }

                        //заменить на возвращение кешированного результата предыдущего запроса
                        ProblemDetails apiRequestConcurrencyError = _pdFactory
                            .CreateProblemDetails(context.HttpContext, StatusCodes.Status409Conflict,
                            "Запрос уже существует.", null, null, context.HttpContext.Request.Path);

                        context.Result = new ConflictObjectResult(apiRequestConcurrencyError);
                        return;
                    }

                    ResourceExecutedContext executedContext = await next.Invoke();

                    int statusCode = context.HttpContext.Response.StatusCode;
                    request.SetStatusCode(statusCode);
                    Dictionary<string, List<string>> headers = context
                        .HttpContext.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToList());
                    request.SetHeaders(headers);

                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.Converters.Add(new JsonStringEnumConverter());
                    options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
                    options.WriteIndented = false;

                    if (executedContext.Result != null)
                    {
                        request.SetResultType(executedContext.Result.GetType().AssemblyQualifiedName);

                        switch (executedContext.Result)
                        {
                            case CreatedAtRouteResult createdRequestResult:
                            {
                                string body = JsonSerializer.Serialize(createdRequestResult.Value, options);
                                request.SetBody(body);

                                request.SetResultRouteName(createdRequestResult.RouteName);

                                Dictionary<string, string> routeValues = createdRequestResult
                                    .RouteValues.ToDictionary(r => r.Key, r => r.Value.ToString());
                                request.SetResultRouteValues(routeValues);

                                break;
                            }
                            case ObjectResult objectRequestResult:
                            {
                                string body = JsonSerializer.Serialize(objectRequestResult.Value, options);
                                request.SetBody(body);

                                break;
                            }
                            case NoContentResult noContentResult:
                            {
                                break;
                            }
                            case OkResult okResult:
                            {
                                break;
                            }
                            case StatusCodeResult statusCodeResult:
                            case ActionResult actionResult:
                            {
                                break;
                            }
                            default:
                            {
                                throw new NotImplementedException($"Обработка идемпотентности не предусмотрена для результата {executedContext.GetType()}");
                            }
                        }
                    }

                    SetResponse(request);
                }
            }
            else
            {
                await next.Invoke();
            }
        }

        private (bool created, ApiRequest request) GetOrCreateRequest(Guid clientRequestId, string method, string path, string query)
        {
            ApiRequest existingRequest = _cacheService.GetApiRequestFromCache<ApiRequest>(clientRequestId);

            if (existingRequest != null)
            {
                return (false, existingRequest);
            }
            else
            {
                ApiRequest apiRequest = new ApiRequest(clientRequestId);

                apiRequest.SetMethod(method);
                apiRequest.SetPath(path);
                apiRequest.SetQuery(query);

                _cacheService.Add(apiRequest, clientRequestId);

                return (true, apiRequest);
            }
        }

        private void SetResponse(ApiRequest request)
        {
            _cacheService.Add(request, request.ApiRequestID);
        }
    }
}
