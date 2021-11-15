using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace YA.ServiceTemplate.OpenApi.OperationFilters;

/// <summary>
/// An Open API operation filter used to document the implicit API version parameter.
/// </summary>
/// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
/// Once they are fixed and published, this class can be removed. See:
/// - https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/412
/// - https://github.com/domaindrivendev/Swashbuckle.AspNetCore/pull/413</remarks>
public class ApiVersionOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        ApiDescription apiDescription = context.ApiDescription;
        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters is null)
        {
            return;
        }

        foreach (OpenApiParameter parameter in operation.Parameters)
        {
            ApiParameterDescription description = apiDescription.ParameterDescriptions
                .First(x => string.Equals(x.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (parameter.Description is null)
            {
                parameter.Description = description.ModelMetadata?.Description;
            }

            if (parameter.Schema.Default is null && description.DefaultValue != null)
            {
                parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
            }

            parameter.Required |= description.IsRequired;
        }
    }
}
