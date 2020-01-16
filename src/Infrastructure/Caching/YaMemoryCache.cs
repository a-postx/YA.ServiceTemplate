using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Infrastructure.Caching
{
    public class YaMemoryCache<T>
    {
        private MemoryCache _cache;

        protected void SetOptions(MemoryCacheOptions options)
        {
            _cache = new MemoryCache(options);
        }

        public async Task<(bool created, T)> GetOrCreateAsync(object key, Func<Task<T>> createItem, MemoryCacheEntryOptions options)
        {
            bool appEventExists = _cache.TryGetValue(key, out T cacheEntry);

            if (!appEventExists)
            {
                T newItem = await createItem();

                MemoryCacheEntryOptions cacheEntryOptions = options;

                _cache.Set(key, newItem, cacheEntryOptions);

                return (true, newItem);
            }
            else
            {
                return (false, cacheEntry);
            }
        }

        public T Set(object key, T cacheEntry, MemoryCacheEntryOptions options) => _cache.Set(key, cacheEntry, options);

        public T Get(object key) => (T)_cache.Get(key);

        public T Update(object key, T newCacheEntry, MemoryCacheEntryOptions options)
        {
            _cache.Remove(key);
            return _cache.Set(key, newCacheEntry, options);
        }
    }
}
