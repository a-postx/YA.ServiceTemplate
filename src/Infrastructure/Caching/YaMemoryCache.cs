using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Infrastructure.Caching
{
    public class YaMemoryCache
    {
        private MemoryCache _cache;

        protected void SetOptions(MemoryCacheOptions options)
        {
            _cache = new MemoryCache(options);
        }

        public async Task<(bool created, T type)> GetOrCreateAsync<T>(object key, Func<Task<T>> createItem, MemoryCacheEntryOptions options) where T : class
        {
            bool itemExists = _cache.TryGetValue(key, out T cacheEntry);

            if (!itemExists)
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

        public T Set<T>(object key, T cacheEntry, MemoryCacheEntryOptions options) where T : class
        {
            return _cache.Set(key, cacheEntry, options) as T;
        }

        public T Get<T>(object key) where T : class
        {
            return (T)_cache.Get(key);
        }

        public T Update<T>(object key, T newCacheEntry, MemoryCacheEntryOptions options) where T : class
        {
            _cache.Remove(key);
            return _cache.Set(key, newCacheEntry, options);
        }
    }
}
