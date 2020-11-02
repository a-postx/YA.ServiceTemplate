using System;
using System.Collections;
using System.Collections.Generic;
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
    public static class HealthResponse
    {
        /// <summary>
        /// Returns a json health status object.
        /// </summary>
        /// <returns>A 200 OK response.</returns>
        [SwaggerResponse(StatusCodes.Status200OK, "Write response in JSON format.")]
        public static Task WriteResponseAsync(HttpContext httpContext, HealthReport result)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            httpContext.Response.ContentType = "application/json";

            List<JProperty> resultEntries = new List<JProperty>();

            foreach (KeyValuePair<string, HealthReportEntry> entry in result.Entries)
            {
                JProperty entryStatus = new JProperty("status", entry.Value.Status.ToString());
                JProperty entryDescription = new JProperty("description", entry.Value.Description);

                List<JProperty> dataList = new List<JProperty>();
                List<JProperty> additionalDataProperties = new List<JProperty>();

                foreach (var dataEntry in entry.Value.Data)
                {
                    object itemValue = null;

                    if (dataEntry.Value is IEnumerable ienum)
                    {
                        foreach (object item in ienum)
                        {
                            if (item is KeyValuePair<string, object> pair)
                            {
                                JProperty itemProp = new JProperty(pair.Key, pair.Value.ToString());
                                additionalDataProperties.Add(itemProp);
                            }
                        }
                    }
                    else
                    {
                        itemValue = dataEntry.Value;
                    }

                    JProperty dataEntryItem = new JProperty(dataEntry.Key, itemValue);
                    dataList.Add(dataEntryItem);
                }

                if (additionalDataProperties.Count > 0 && dataList.Count == 1 && dataList[0].Name == "Endpoints")
                {
                    JObject additionalObject = new JObject();

                    foreach (JProperty item in additionalDataProperties)
                    {
                        additionalObject.Add(new JProperty(item.Name, item.Value));
                    }

                    dataList[0].Value = additionalObject;
                }

                JObject dataObject = new JObject(dataList);

                JProperty entryData = new JProperty("data", dataObject);

                JObject content = new JObject(entryStatus, entryDescription, entryData);
                JProperty propEntry = new JProperty(entry.Key, content);
                resultEntries.Add(propEntry);
            }

            JObject resultsValue = new JObject(resultEntries);

            JProperty status = new JProperty("status", result.Status.ToString());
            JProperty results = new JProperty("results", resultsValue);

            JObject json = new JObject(status, results);

            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
