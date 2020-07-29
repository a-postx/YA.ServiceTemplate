using Microsoft.AspNetCore.Mvc;

namespace YA.ServiceTemplate.Application.Models.Dto
{
    /// <summary>
    /// Детали проблемы АПИ-запроса (расширенный вариант RFC7807)
    /// </summary>
    public class ApiProblemDetails : ProblemDetails
    {
        private ApiProblemDetails() { }
        public ApiProblemDetails(string type, int? status, string instance, string title, string detail, string correlationId, string traceId = null)
        {
            Type = type;
            Status = status;
            Instance = instance;
            Title = title;
            Detail = detail;
            CorrelationId = correlationId;
            TraceId = traceId;
        }

        /// <summary>
        /// Идентификатор корелляции
        /// </summary>
        public string CorrelationId { get; private set; }

        /// <summary>
        /// Идентификатор трассировки
        /// </summary>
        public string TraceId { get; private set; }
    }
}