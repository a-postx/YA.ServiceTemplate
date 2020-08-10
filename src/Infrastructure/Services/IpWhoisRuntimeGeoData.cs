using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Infrastructure.Services.GeoDataModels;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    public class IpWhoisRuntimeGeoData : IRuntimeGeoDataService
    {
        public IpWhoisRuntimeGeoData(ILogger<IpWhoisRuntimeGeoData> logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            ProviderUrl = "https://ipwhois.app/";
        }

        private readonly ILogger<IpWhoisRuntimeGeoData> _log;

        private string ProviderUrl { get; set; }

        public async Task<Countries> GetCountryCodeAsync(CancellationToken cancellationToken)
        {
            Countries result = Countries.UN;

            IpWhoisGeoData geoData = await GetDataAsync(cancellationToken);

            if (geoData != null)
            {
                if (Enum.TryParse(geoData.country_code, out Countries parseResult))
                {
                    result = parseResult;
                    _log.LogInformation("Geodata received successfully, runtime country is {Country}", result.ToString());
                }
            }

            return result;
        }

        private async Task<IpWhoisGeoData> GetDataAsync(CancellationToken cancellationToken)
        {
            IpWhoisGeoData result = null;

            try
            {
                using (HttpClient client = Utils.GetHttpClient(General.AppHttpUserAgent))
                {
                    client.BaseAddress = new Uri(ProviderUrl);

                    using (HttpResponseMessage response = await client.GetAsync(new Uri("json/?lang=ru&objects=country_code", UriKind.Relative), cancellationToken))
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            IpWhoisGeoData data = await response.Content.ReadAsJsonAsync<IpWhoisGeoData>();

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
