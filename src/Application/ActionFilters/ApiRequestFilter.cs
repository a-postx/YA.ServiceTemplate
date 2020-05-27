using CorrelationId.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionFilters
{
    /// <summary>
    /// Idempotency filter: saves request and result to return the same result in case of duplicate request.
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
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                string method = context.HttpContext.Request.Method;

                if (Guid.TryParse(_correlationContextAccessor.CorrelationContext.CorrelationId, out Guid correlationId))
                {
                    (bool requestCreated, ApiRequest request) = await _apiRequestTracker.GetOrCreateRequestAsync(correlationId, method, cts.Token);

                    if (!requestCreated)
                    {
                        ApiProblemDetails apiError = new ApiProblemDetails("https://tools.ietf.org/html/rfc7231#section-6.5.8", StatusCodes.Status409Conflict,
                                context.HttpContext.Request.HttpContext.Request.Path.Value, "Api call is already exist.", null, request.ApiRequestId.ToString(),
                                context.HttpContext.Request.HttpContext.TraceIdentifier);

                        context.Result = new ConflictObjectResult(apiError);
                        return;
                    }
                }
                else
                {
                    context.Result = new BadRequestResult();
                    return;
                }
            }

            await next.Invoke();
        }

        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
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
                                    ApiRequestResult apiRequestResult = new ApiRequestResult
                                    {
                                        StatusCode = objectRequestResult.StatusCode,
                                        Body = JToken.Parse(JsonConvert.SerializeObject(objectRequestResult.Value)).ToString(Formatting.None)
                                    };

                                    await _apiRequestTracker.SetResultAsync(request, apiRequestResult, cts.Token);
                                    break;
                                }
                            case OkResult okResult:
                                {
                                    ApiRequestResult result = new ApiRequestResult
                                    {
                                        StatusCode = okResult.StatusCode
                                    };

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
