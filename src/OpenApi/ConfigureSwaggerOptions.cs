using System.Reflection;
using Delobytes.AspNetCore.Swagger;
using Delobytes.AspNetCore.Swagger.OperationFilters;
using Delobytes.AspNetCore.Swagger.SchemaFilters;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using YA.ServiceTemplate.OpenApi.OperationFilters;
using YA.ServiceTemplate.Options;

namespace YA.ServiceTemplate.OpenApi;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider,
        IOptions<IdempotencyOptions> idempotencyOptions)
    {
        _provider = provider;
        _idempotencyOptions = idempotencyOptions.Value;
    }

    private readonly IApiVersionDescriptionProvider _provider;
    private readonly IdempotencyOptions _idempotencyOptions;

    public void Configure(SwaggerGenOptions options)
    {
        Assembly assembly = typeof(Startup).Assembly;

        options.DescribeAllParametersInCamelCase();
        options.EnableAnnotations();

        // Add the XML comment file for this assembly, so its contents can be displayed.
        options.IncludeXmlCommentsIfExists(assembly);

        options.OperationFilter<ApiVersionOperationFilter>();

        if (_idempotencyOptions.IdempotencyFilterEnabled.HasValue && _idempotencyOptions.IdempotencyFilterEnabled.Value)
        {
            options.OperationFilter<IdempotencyKeyOperationFilter>(_idempotencyOptions.IdempotencyHeader);
        }

        options.OperationFilter<ContentTypeOperationFilter>();
        // Show an example model for JsonPatchDocument<T>.
        options.SchemaFilter<JsonPatchDocumentSchemaFilter>();

        string assemblyProduct = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        string assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>().Description;

        foreach (ApiVersionDescription apiVersionDescription in _provider.ApiVersionDescriptions)
        {
            OpenApiInfo info = new OpenApiInfo()
            {
                Title = assemblyProduct,
                Description = apiVersionDescription.IsDeprecated
                    ? $"{assemblyDescription} This API version has been deprecated."
                    : assemblyDescription,
                Version = apiVersionDescription.ApiVersion.ToString()
            };
            options.SwaggerDoc(apiVersionDescription.GroupName, info);
        }
    }
}
