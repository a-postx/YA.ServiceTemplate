using FluentValidation.AspNetCore;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Application.Services;

/// <summary>
/// Фабрика стандартного вывода деталей проблемы HTTP-запроса
/// </summary>
public class YaProblemDetailsFactory : ProblemDetailsFactory, IProblemDetailsFactory
{
    /// <inheritdoc />
    public YaProblemDetailsFactory(IOptions<ApiBehaviorOptions> options)
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
        ArgumentNullException.ThrowIfNull(httpContext);

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
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(modelStateDictionary);

        statusCode ??= StatusCodes.Status400BadRequest;
        title ??= "Произошла ошибка валидации данных модели.";
        detail ??= "Обратитесь к свойству errors за дополнительной информацией.";
        instance ??= httpContext.Request.Path;
        type ??= "https://tools.ietf.org/html/rfc7231#section-6.5.1";

        ValidationProblemDetails problemDetails = new ValidationProblemDetails(modelStateDictionary)
        {
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = instance,
            Type = type
        };

        ApplyProblemDetailsDefaults(problemDetails, statusCode.Value);
        EnrichProblemDetailsWithContext(httpContext, problemDetails);

        return problemDetails;
    }

    public ValidationProblemDetails CreateValidationProblemDetails(HttpContext httpContext,
        ValidationResult validationResult,
        int? statusCode = null,
        string title = null,
        string type = null,
        string detail = null,
        string instance = null)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(validationResult);

        IActionContextAccessor actionCtx = httpContext.RequestServices.GetRequiredService<IActionContextAccessor>();

        validationResult.AddToModelState(actionCtx.ActionContext.ModelState, "");
        return CreateValidationProblemDetails(httpContext, actionCtx.ActionContext.ModelState, statusCode, title, type, detail, instance);
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
