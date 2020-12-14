using System.Collections.Generic;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;
using YA.ServiceTemplate.Application.Models.ViewModels;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IPaginatedResultFactory
    {
        PaginatedResultVm<T> GetPaginatedResult<T>(PageOptions pageOptions, bool hasNextPage, bool hasPreviousPage, int totalCount, string startCursor, string endCursor, string routeName, List<T> itemVms) where T : class;
    }
}
