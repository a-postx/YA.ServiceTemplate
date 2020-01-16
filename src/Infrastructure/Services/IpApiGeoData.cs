using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    public class IpApiGeoData : IGeoDataService
    {
        public IpApiGeoData(ILogger<IpApiGeoData> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            ProviderUrl = "http://ip-api.com";
        }

        private readonly ILogger<IpApiGeoData> _log;

        private string ProviderUrl { get; set; }
        private GeoData Data { get; set; }

        public async Task<Countries> GetCountryCodeAsync()
        {
            Countries result = Countries.UN;

            Data = await GetGeoDataAsync();

            if (Data != null)
            {
                bool success = Enum.TryParse(Data.CountryCode, out Countries parseResult);

                if (success)
                {
                    result = parseResult;
                    _log.LogInformation("Geodata received successfully, application country is {Country}", result.ToString());
                }
            }

            return result;
        }

        private async Task<GeoData> GetGeoDataAsync()
        {
            GeoData result = null;

            try
            {
                using (HttpClient client = Utils.GetHttpClient(General.AppHttpUserAgent))
                {
                    client.BaseAddress = new Uri(ProviderUrl);

                    using (HttpResponseMessage response = await client.GetAsync("/json"))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            GeoData data = await response.Content.ReadAsJsonAsync<GeoData>();

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
            catch (Exception e)
            {
                _log.LogError("Error getting geodata: {Error}", e);
            }

            return result;
        }
    }
}
