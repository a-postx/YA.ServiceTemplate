using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Infrastructure.Services.GeoDataModels;

namespace YA.ServiceTemplate.Infrastructure.Services
{
    public class IpWhoisRuntimeGeoData : IRuntimeGeoDataService
    {
        public IpWhoisRuntimeGeoData(ILogger<IpWhoisRuntimeGeoData> logger, IHttpClientFactory httpClientFactory)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        private readonly ILogger<IpWhoisRuntimeGeoData> _log;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _providerUrl = "https://ipwhois.app/";

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

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Handler is disposed with HttpClient")]
        private async Task<IpWhoisGeoData> GetDataAsync(CancellationToken cancellationToken)
        {
            IpWhoisGeoData result = null;

            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_providerUrl);
                string userAgent = $"{Program.AppName}/{Program.AppVersion}";
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                client.Timeout = TimeSpan.FromSeconds(60);

                HttpResponseMessage response = await client.GetAsync(new Uri("json/?lang=ru&objects=country_code", UriKind.Relative), cancellationToken);
                response.EnsureSuccessStatusCode();

                using (Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    IpWhoisGeoData data = await JsonSerializer
                        .DeserializeAsync<IpWhoisGeoData>(responseStream, null, cancellationToken);

                    if (data != null)
                    {
                        result = data;
                    }
                    else
                    {
                        _log.LogWarning("No geodata available.");
                    }
                }
            }
            catch (TaskCanceledException ex)
            {
                _log.LogWarning($"Unable to get runtime geodata: {ex.Message}");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error getting geodata");
            }

            return result;
        }
    }
}
