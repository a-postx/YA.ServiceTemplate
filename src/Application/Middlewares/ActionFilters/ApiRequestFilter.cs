using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.Application.Middlewares.ActionFilters
{
    /// <summary>
    /// Фильтр идемпотентности: не допускает запросов без корелляционного идентификатора
    /// и сохраняет запрос и результат чтобы вернуть тот же ответ в случае запроса-дубликата.
    /// </summary>
    public class ApiRequestFilter : ActionFilterAttribute
    {
        public ApiRequestFilter(IApiRequestTracker apiRequestTracker,
            IRuntimeContextAccessor runtimeContextAccessor,
            IOptions<GeneralOptions> options)
        {
            _runtimeCtx = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));
            _apiRequestTracker = apiRequestTracker ?? throw new ArgumentNullException(nameof(apiRequestTracker));
            _generalOptions = options.Value;
        }

        private readonly IRuntimeContextAccessor _runtimeCtx;
        private readonly IApiRequestTracker _apiRequestTracker;
        private readonly GeneralOptions _generalOptions;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string method = context.HttpContext.Request.Method;

            Guid correlationId = _runtimeCtx.GetCorrelationId();

            if (correlationId != Guid.Empty)
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(Timeouts.ApiRequestFilterMs))
                {
                    (bool requestCreated, ApiRequest request) = await _apiRequestTracker.GetOrCreateRequestAsync(correlationId, method, cts.Token);

                    if (!requestCreated)
                    {
                        ProblemDetails apiError = new ProblemDetails
                        {
                            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                            Status = StatusCodes.Status409Conflict,
                            Instance = context.HttpContext.Request.HttpContext.Request.Path.Value,
                            Title = "Запрос уже существует.",
                            Detail = null
                        };
                        apiError.Extensions.Add("correlationId", request.ApiRequestID.ToString());
                        apiError.Extensions.Add("traceId", _runtimeCtx.GetTraceId().ToString());

                        context.Result = new ConflictObjectResult(apiError);
                        return;
                    }
                }
            }
            else
            {
                ProblemDetails problemDetails = new ProblemDetails()
                {
                    Instance = context.HttpContext.Request.Path,
                    Status = StatusCodes.Status400BadRequest,
                    Title = $"Запрос не содержит заголовка {_generalOptions.CorrelationIdHeader} или значение в нём неверно."
                };
                problemDetails.Extensions.Add("traceId", _runtimeCtx.GetTraceId().ToString());

                context.Result = new BadRequestObjectResult(problemDetails);
                return;
            }

            await next.Invoke();
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            string method = context.HttpContext.Request.Method;
            Guid correlationId = _runtimeCtx.GetCorrelationId();

            if (correlationId != Guid.Empty)
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(Timeouts.ApiRequestFilterMs))
                {
                    (bool requestCreated, ApiRequest request) = await _apiRequestTracker.GetOrCreateRequestAsync(correlationId, method, cts.Token);

                    if (request != null)
                    {
                        switch (context.Result)
                        {
                            case ObjectResult objectRequestResult when objectRequestResult.Value is ProblemDetails apiError:
                                ////if (apiError.Code == ApiErrorCodes.DUPLICATE_API_CALL)
                                ////{
                                ////    if (request.ResponseBody != null)
                                ////    {
                                ////        try
                                ////        {
                                ////            JToken token = JToken.Parse(request.ResponseBody);
                                ////            JObject json = JObject.Parse((string)token);

                                ////            ObjectResult previousResult = new ObjectResult(json)
                                ////            {
                                ////                StatusCode = request.ResponseStatusCode
                                ////            };

                                ////            context.Result = previousResult;
                                ////        }
                                ////        catch (JsonReaderException)
                                ////        {
                                ////            //ignore object parsing exception as we return ApiError object in this case
                                ////        }
                                ////    }
                                ////}
                                break;
                            case ObjectResult objectRequestResult:
                                {
                                    string body = JToken.Parse(JsonConvert.SerializeObject(objectRequestResult.Value)).ToString(Formatting.None);
                                    ApiRequestResult apiRequestResult = new ApiRequestResult(objectRequestResult.StatusCode, body);

                                    await _apiRequestTracker.SetResultAsync(request, apiRequestResult, cts.Token);
                                    break;
                                }
                            case OkResult okResult:
                                {
                                    ApiRequestResult result = new ApiRequestResult(okResult.StatusCode, null);

                                    await _apiRequestTracker.SetResultAsync(request, result, cts.Token);
                                    break;
                                }
                        }
                    }
                }
            }

            await next.Invoke();
        }
    }
}
