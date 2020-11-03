using System.Text.Json;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using YA.ServiceTemplate.Application.Models.SaveModels;

namespace YA.ServiceTemplate.Application.Models.ModelSchemaFilters
{
    public class CarSmSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema model, SchemaFilterContext context)
        {
            CarSm carSm = new CarSm()
            {
                Cylinders = 4,
                Brand = "Toyota",
                Model = "Hilux"
            };

            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

            model.Default = new OpenApiString(JsonSerializer.Serialize(carSm, options));
            model.Example = new OpenApiString(JsonSerializer.Serialize(carSm, options));
        }
    }
}
