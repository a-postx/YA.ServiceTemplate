using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;

namespace YA.ServiceTemplate.Health
{
    /// <summary>
    /// JSON formatter for health responses.
    /// </summary>
    public class HealthResponse
    {
        /// <summary>
        /// Returns a json health status object.
        /// </summary>
        /// <returns>A 200 OK response.</returns>
        [SwaggerResponse(StatusCodes.Status200OK, "Write response in JSON format.")]
        public static Task WriteResponseAsync(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            JObject json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));

            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
