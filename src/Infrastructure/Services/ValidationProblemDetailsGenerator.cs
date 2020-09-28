using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    public class ValidationProblemDetailsGenerator : IValidationProblemDetailsGenerator
    {
        public ValidationProblemDetailsGenerator(IRuntimeContextAccessor runtimeCtx, IActionContextAccessor actionCtx)
        {
            _runtimeCtx = runtimeCtx ?? throw new ArgumentNullException(nameof(runtimeCtx));
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
        }

        private readonly IRuntimeContextAccessor _runtimeCtx;
        private readonly IActionContextAccessor _actionCtx;

        private ValidationProblemDetails Create(ModelStateDictionary modelState)
        {
            ValidationProblemDetails problemDetails = new ValidationProblemDetails(modelState)
            {
                Title = "Произошла ошибка валидации данных модели.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Обратитесь к свойству errors за дополнительной информацией.",
                Instance = _actionCtx.ActionContext.HttpContext.Request.Path
            };

            problemDetails.Extensions.Add("traceId", _runtimeCtx.GetTraceId());
            problemDetails.Extensions.Add("correlationId", _runtimeCtx.GetCorrelationId());

            return problemDetails;
        }

        public ValidationProblemDetails Generate(ValidationResult validationResult)
        {
            validationResult.AddToModelState(_actionCtx.ActionContext.ModelState, "");
            return Create(_actionCtx.ActionContext.ModelState);
        }

        public ValidationProblemDetails Generate(ModelStateDictionary modelState)
        {
            return Create(modelState);
        }
    }
}
