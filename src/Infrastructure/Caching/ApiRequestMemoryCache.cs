using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using YA.ServiceTemplate.Application.Interfaces;

namespace YA.ServiceTemplate.Infrastructure.Caching
{
    public class ApiRequestMemoryCache : YaMemoryCache, IApiRequestMemoryCache
    {
        public ApiRequestMemoryCache()
        {
            MemoryCacheOptions cacheOptions = new MemoryCacheOptions { SizeLimit = 256 };
            SetOptions(cacheOptions);
        }

        private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetPriority(CacheItemPriority.High)
                    .SetSlidingExpiration(TimeSpan.FromSeconds(120))
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(300));

        public async Task<(bool created, T request)> GetOrCreateAsync<T>(Guid key, Func<Task<T>> createItem) where T : class
        {
            (bool created, T request) result = await base.GetOrCreateAsync(key, createItem, CacheOptions);
            return result;
        }

        public void Add<T>(T request, Guid key) where T : class
        {
            base.Set(key, request, CacheOptions);
        }

        public T GetApiRequestFromCache<T>(Guid key) where T : class
        {
            return base.Get<T>(key);
        }

        public void Update<T>(T request, Guid key) where T : class
        {
            base.Update(key, request, CacheOptions);
        }
    }
}
