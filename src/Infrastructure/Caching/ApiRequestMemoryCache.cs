using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Constants;

namespace YA.ServiceTemplate.Infrastructure.Caching
{
    public class ApiRequestMemoryCache : YaMemoryCache, IApiRequestMemoryCache
    {
        public ApiRequestMemoryCache()
        {
            MemoryCacheOptions options = new MemoryCacheOptions { SizeLimit = General.ApiRequestsCacheSize };
            SetOptions(options);
        }

        private static readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetPriority(CacheItemPriority.High)
                    .SetSlidingExpiration(TimeSpan.FromSeconds(General.ApiRequestCacheSlidingExpirationSec))
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(General.ApiRequestCacheAbsoluteExpirationSec));

        public async Task<(bool created, T request)> GetOrCreateAsync<T>(Guid key, Func<Task<T>> createItem) where T : class
        {
            (bool created, T request) result = await base.GetOrCreateAsync(key, createItem, _cacheOptions);
            return result;
        }

        public void Add<T>(T request, Guid key) where T : class
        {
            base.Set(key, request, _cacheOptions);
        }

        public T GetApiRequestFromCache<T>(Guid key) where T : class
        {
            return base.Get<T>(key);
        }

        public void Update<T>(T request, Guid key) where T : class
        {
            base.Update(key, request, _cacheOptions);
        }
    }
}
