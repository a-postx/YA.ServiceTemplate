using System;
using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace YA.ServiceTemplate.OperationFilters
{
    /// <summary>
    /// Adds a Swashbuckle <see cref="OpenApiExample"/> to all available operations with a description of X-Correlation-ID
    /// HTTP header and default GUID value.
    /// </summary>
    /// <seealso cref="IOperationFilter" />
    public class CorrelationIdOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Apply the specified operation.
        /// </summary>
        /// <param name="operation">Operation.</param>
        /// <param name="context">Context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<OpenApiParameter>();
            }

            operation.Parameters.Add(
                new OpenApiParameter()
                {
                    Description = "Used to identify HTTP request: the ID will correlate HTTP request between server and client.",
                    In = ParameterLocation.Header,
                    Name = "X-Correlation-ID",
                    Required = true,
                    Schema = new OpenApiSchema
                    {
                        Default = new OpenApiString(Guid.NewGuid().ToString()),
                        Type = "string",
                    },
                });
        }
    }
}
