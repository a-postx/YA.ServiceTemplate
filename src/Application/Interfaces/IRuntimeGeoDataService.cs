using System.Threading;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public enum Countries
    {
        UN = 0,
        RU = 1,
        CN = 2,
        US = 4,
        DE = 8,
        FR = 16,
        IE = 32,
        GB = 64,
        SG = 128
    }
    /// <summary>
    /// Retrieves geodata for the current service.
    /// </summary>
    public interface IRuntimeGeoDataService
    {
        /// <summary>
        /// Country code of the service location (ISO 3166).
        /// </summary>
        Task<Countries> GetCountryCodeAsync(CancellationToken cancellationToken);
    }
}
