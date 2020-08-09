using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Infrastructure.Services.GeoDataModels;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    public class SypexRuntimeGeoData : IRuntimeGeoDataService
    {
        public SypexRuntimeGeoData(ILogger<SypexRuntimeGeoData> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            ProviderUrl = "https://api.sypexgeo.net/json";
        }

        private readonly ILogger<SypexRuntimeGeoData> _log;

        private string ProviderUrl { get; set; }

        public async Task<Countries> GetCountryCodeAsync()
        {
            Countries result = Countries.UN;

            SypexGeoData geoData = await GetGeoDataAsync();

            if (geoData != null)
            {
                if (Enum.TryParse(geoData.country.iso, out Countries parseResult))
                {
                    result = parseResult;
                    _log.LogInformation("Geodata received successfully, runtime country is {Country}", result.ToString());
                }
            }

            return result;
        }

        private async Task<SypexGeoData> GetGeoDataAsync()
        {
            SypexGeoData result = null;

            try
            {
                using (HttpClient client = Utils.GetHttpClient(General.AppHttpUserAgent))
                {
                    client.BaseAddress = new Uri(ProviderUrl);

                    using (HttpResponseMessage response = await client.GetAsync(new Uri("/json", UriKind.Relative)))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            SypexGeoData data = await response.Content.ReadAsJsonAsync<SypexGeoData>();

                            if (data != null)
                            {
                                result = data;
                            }
                            else
                            {
                                _log.LogWarning("No geodata available.");
                            }
                        }
                        else
                        {
                            _log.LogWarning("No geodata available, response status code is {Code}.", response.StatusCode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error getting geodata");
            }

            return result;
        }
    }
}
