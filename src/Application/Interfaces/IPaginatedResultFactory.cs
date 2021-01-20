using System.Collections.Generic;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;
using YA.ServiceTemplate.Application.Models.ViewModels;

namespace YA.ServiceTemplate.Application.Interfaces
{
    public interface IPaginatedResultFactory
    {
        PaginatedResultVm<T> GetCursorPaginatedResult<T>(PageOptionsCursor pageOptions, bool hasNextPage, bool hasPreviousPage, int totalCount, string startCursor, string endCursor, string routeName, ICollection<T> items) where T : class;
    }
}
