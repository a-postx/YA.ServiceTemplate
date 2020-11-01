using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IProblemDetailsFactory
    {
        ProblemDetails CreateProblemDetails(HttpContext context, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null);
        ValidationProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationResult validationResult, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null);
    }
}
