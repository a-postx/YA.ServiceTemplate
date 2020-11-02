using System.Collections.Generic;

namespace YA.ServiceTemplate.Core
{
    public class PaginatedResult<T> where T : class
    {
        public PaginatedResult(bool hasNextPage, bool hasPreviousPage, int totalCount, ICollection<T> items)
        {
            HasNextPage = hasNextPage;
            HasPreviousPage = hasPreviousPage;
            TotalCount = totalCount;
            Items = items;
        }

        public bool HasNextPage { get; private set; }
        public bool HasPreviousPage { get; private set; }
        public int TotalCount { get; private set; }
        public ICollection<T> Items { get; private set; }
    }
}
