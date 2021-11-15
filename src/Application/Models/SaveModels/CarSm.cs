using Swashbuckle.AspNetCore.Annotations;
using YA.ServiceTemplate.Application.Models.ModelSchemaFilters;

namespace YA.ServiceTemplate.Application.Models.SaveModels;

/// <summary>
/// Car model coming from external API call.
/// </summary>
[SwaggerSchemaFilter(typeof(CarSmSchemaFilter))]
public class CarSm
{
    /// <summary>
    /// Number of motor cylinders.
    /// </summary>
    public int Cylinders { get; set; }

    /// <summary>
    /// Car brand.
    /// </summary>
    public string Brand { get; set; }

    /// <summary>
    /// Car model.
    /// </summary>
    public string Model { get; set; }
}
