using System;
using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application
{
    /// <summary>
	/// Обработчик стандартного вывода деталей проблемы HTTP-запроса
	/// </summary>
	public class CustomProblemDetailsFactory : ProblemDetailsFactory, IProblemDetailsFactory
    {
        /// <inheritdoc />
        public CustomProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        private readonly ApiBehaviorOptions _options;

        /// <inheritdoc />
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            statusCode ??= StatusCodes.Status500InternalServerError;

            ProblemDetails problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

            ApplyProblemDetailsDefaults(problemDetails, statusCode.Value);
            EnrichProblemDetailsWithContext(httpContext, problemDetails);

            return problemDetails;
        }

        /// <inheritdoc />
        public override ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            if (modelStateDictionary == null)
            {
                throw new ArgumentNullException(nameof(modelStateDictionary));
            }

            statusCode ??= StatusCodes.Status400BadRequest;

            ValidationProblemDetails problemDetails = new ValidationProblemDetails(modelStateDictionary)
            {
                Title = "Произошла ошибка валидации данных модели.",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Обратитесь к свойству errors за дополнительной информацией.",
                Instance = httpContext.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = title;
            }

            ApplyProblemDetailsDefaults(problemDetails, statusCode.Value);
            EnrichProblemDetailsWithContext(httpContext, problemDetails);

            return problemDetails;
        }

        public ValidationProblemDetails CreateValidationProblemDetails(HttpContext context,
            ValidationResult validationResult,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            IActionContextAccessor actionCtx = context.RequestServices.GetRequiredService<IActionContextAccessor>();

            validationResult.AddToModelState(actionCtx.ActionContext.ModelState, "");
            return CreateValidationProblemDetails(context, actionCtx.ActionContext.ModelState, statusCode, title, type, detail, instance);
        }

        private void ApplyProblemDetailsDefaults(ProblemDetails problemDetails, int statusCode)
        {
            problemDetails.Status ??= statusCode;

            if (_options.ClientErrorMapping.TryGetValue(statusCode, out ClientErrorData clientErrorData))
            {
                problemDetails.Title ??= clientErrorData.Title;
                problemDetails.Type ??= clientErrorData.Link;
            }
        }

        private void EnrichProblemDetailsWithContext(HttpContext context, ProblemDetails problemDetails)
        {
            IRuntimeContextAccessor runtimeCtx = context.RequestServices.GetRequiredService<IRuntimeContextAccessor>();
            Guid correlationId = runtimeCtx.GetCorrelationId();
            string traceId = runtimeCtx.GetTraceId();

            if (correlationId != Guid.Empty)
            {
                problemDetails.Extensions.Add("correlationId", correlationId);
            }

            if (!string.IsNullOrEmpty(traceId))
            {
                problemDetails.Extensions.Add("traceId", traceId);
            }
        }
    }
}
