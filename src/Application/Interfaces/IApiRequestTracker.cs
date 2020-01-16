using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Models.Dto;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IApiRequestTracker
    {
        Task<(bool created, ApiRequest request)> GetOrCreateRequestAsync(Guid correlationId, string method, CancellationToken cancellationToken);
        Task SetResultAsync(ApiRequest request, ApiRequestResult requestResult, CancellationToken cancellationToken);
    }
}