using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace YA.ServiceTemplate.Infrastructure.Caching
{
    public class YaMemoryCache : IDisposable
    {
        private MemoryCache _cache;
        private bool _disposed;

        protected void SetOptions(MemoryCacheOptions options)
        {
            _cache = new MemoryCache(options);
        }

        public async Task<(bool created, T type)> GetOrCreateAsync<T>(object key, Func<Task<T>> createFunc, MemoryCacheEntryOptions options) where T : class
        {
            if (createFunc == null)
            {
                throw new ArgumentNullException(nameof(createFunc));
            }

            bool itemExists = _cache.TryGetValue(key, out T cacheEntry);

            if (!itemExists)
            {
                T newItem = await createFunc();

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

        public void Remove<T>(object key) where T : class
        {
            _cache.Remove(key);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _cache.Dispose();
                }

                _cache = null;

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
