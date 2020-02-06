using System;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IApiRequestMemoryCache
    {
        void Add<T>(T request, Guid key) where T : class;
        void Update<T>(T request, Guid key) where T : class;
        T GetApiRequestFromCache<T>(Guid key) where T : class;
        Task<(bool created, T request)> GetOrCreateAsync<T>(Guid key, Func<Task<T>> createItem) where T : class;
    }
}
