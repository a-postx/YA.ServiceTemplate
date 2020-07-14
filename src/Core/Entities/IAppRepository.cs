using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Core.Entities
{
    public interface IAppRepository
    {
        Task<Car> AddAsync(Car car, CancellationToken cancellationToken);

        Task DeleteAsync(Car car, CancellationToken cancellationToken);

        Task<Car> GetAsync(int carId, CancellationToken cancellationToken);

        Task<List<Car>> GetCarsAsync(
            int? first,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken);

        Task<List<Car>> GetCarsReverseAsync(
            int? last,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken);

        Task<bool> GetHasNextPageAsync(
            int? first,
            DateTimeOffset? createdAfter,
            CancellationToken cancellationToken);

        Task<bool> GetHasPreviousPagAsync(
            int? last,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken);

        Task<int> GetTotalCountAsync(CancellationToken cancellationToken);

        Task<ICollection<Car>> GetPageAsync(int page, int count, CancellationToken cancellationToken);

        Task<(int totalCount, int totalPages)> GetTotalPagesAsync(int count, CancellationToken cancellationToken);

        Task<Car> UpdateAsync(Car car, CancellationToken cancellationToken);


        Task<ApiRequest> CreateApiRequestAsync(ApiRequest request);
        Task<ApiRequest> GetApiRequestAsync(Guid correlationId);
        Task UpdateApiRequestAsync(ApiRequest request);
    }
}
