using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionFilters
{
    /// <summary>
    /// Фильтр идемпотентности: не допускает запросов без корелляционного идентификатора
    /// и сохраняет запрос и результат чтобы вернуть тот же ответ в случае запроса-дубликата.
    /// </summary>
    public sealed class ApiRequestFilter : ActionFilterAttribute
    {
        public ApiRequestFilter(IApiRequestTracker apiRequestTracker, ICorrelationContextAccessor correlationContextAccessor)
        {
            _correlationContextAccessor = correlationContextAccessor ?? throw new ArgumentNullException(nameof(correlationContextAccessor));
            _apiRequestTracker = apiRequestTracker ?? throw new ArgumentNullException(nameof(apiRequestTracker));
        }

        private readonly ICorrelationContextAccessor _correlationContextAccessor;
        private readonly IApiRequestTracker _apiRequestTracker;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Timeouts.ApiRequestFilterMs))
            {
                string method = context.HttpContext.Request.Method;

                if (Guid.TryParse(_correlationContextAccessor.CorrelationContext.CorrelationId, out Guid correlationId))
                {
                    (bool requestCreated, ApiRequest request) = await _apiRequestTracker.GetOrCreateRequestAsync(correlationId, method, cts.Token);

                    if (!requestCreated)
                    {
                        ApiProblemDetails apiError = new ApiProblemDetails("https://tools.ietf.org/html/rfc7231#section-6.5.8", StatusCodes.Status409Conflict,
                                context.HttpContext.Request.HttpContext.Request.Path.Value, "Запрос уже существует.", null, request.ApiRequestId.ToString(),
                                context.HttpContext.Request.HttpContext.TraceIdentifier);

                        context.Result = new ConflictObjectResult(apiError);
                        return;
                    }
                }
                else
                {
                    ProblemDetails problemDetails = new ProblemDetails()
                    {
                        Instance = context.HttpContext.Request.Path,
                        Status = StatusCodes.Status400BadRequest,
                        Detail = $"Запрос не содержит заголовка {General.CorrelationIdHeader} или значение в нём неверно."
                    };

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }

            await next.Invoke();
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource(Timeouts.ApiRequestFilterMs))
            {
                string method = context.HttpContext.Request.Method;

                if (Guid.TryParse(_correlationContextAccessor.CorrelationContext.CorrelationId, out Guid correlationId))
                {
                    (bool requestCreated, ApiRequest request) = await _apiRequestTracker.GetOrCreateRequestAsync(correlationId, method, cts.Token);

                    if (request != null)
                    {
                        switch (context.Result)
                        {
                            case ObjectResult objectRequestResult when objectRequestResult.Value is ApiProblemDetails apiError:
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
