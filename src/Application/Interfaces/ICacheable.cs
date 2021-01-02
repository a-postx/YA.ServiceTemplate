using System;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface ICacheable
    {
        public string CacheKey { get; }
        public TimeSpan AbsoluteExpiration { get; }
    }
}
