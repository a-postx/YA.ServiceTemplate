using Delobytes.AspNetCore;
using Delobytes.Mapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Commands
{
    public class GetCarPageCommand : IGetCarPageCommand
    {
        public GetCarPageCommand(ILogger<GetCarPageCommand> logger, IAppRepository carRepository, IMapper<Car, CarVm> carMapper, IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
            _carMapper = carMapper ?? throw new ArgumentNullException(nameof(carMapper));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _linkGenerator = linkGenerator ?? throw new ArgumentNullException(nameof(linkGenerator));
        }

        private readonly ILogger<GetCarPageCommand> _log;
        private readonly IAppRepository _carRepository;
        private readonly IMapper<Car, CarVm> _carMapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly LinkGenerator _linkGenerator;

        public async Task<IActionResult> ExecuteAsync(PageOptions pageOptions, CancellationToken cancellationToken)
        {
            if (pageOptions is null)
            {
                throw new ArgumentNullException(nameof(pageOptions));
            }

            pageOptions.First = !pageOptions.First.HasValue && !pageOptions.Last.HasValue ? General.DefaultPageSizeForPagination : pageOptions.First;
            DateTimeOffset? createdAfter = Cursor.FromCursor<DateTimeOffset?>(pageOptions.After);
            DateTimeOffset? createdBefore = Cursor.FromCursor<DateTimeOffset?>(pageOptions.Before);
            
            Task<List<Car>> getCarsTask = GetCarsAsync(pageOptions.First, pageOptions.Last, createdAfter, createdBefore, cancellationToken);
            Task<bool> getHasNextPageTask = GetHasNextPageAsync(pageOptions.First, createdAfter, createdBefore, cancellationToken);
            Task<bool> getHasPreviousPageTask = GetHasPreviousPageAsync(pageOptions.Last, createdAfter, createdBefore, cancellationToken);
            Task<int> totalCountTask = _carRepository.GetTotalCountAsync(cancellationToken);

            await Task.WhenAll(getCarsTask, getHasNextPageTask, getHasPreviousPageTask, totalCountTask).ConfigureAwait(false);
            List<Car> cars = await getCarsTask.ConfigureAwait(false);
            bool hasNextPage = await getHasNextPageTask.ConfigureAwait(false);
            bool hasPreviousPage = await getHasPreviousPageTask.ConfigureAwait(false);
            int totalCount = await totalCountTask.ConfigureAwait(false);

            if (cars == null)
            {
                return new NotFoundResult();
            }

            (string startCursor, string endCursor) = Cursor.GetFirstAndLastCursor(cars, x => x.Created);
            List<CarVm> carViewModels = _carMapper.MapList(cars);

            PaginatedResult<CarVm> paginatedResult = new PaginatedResult<CarVm>(_linkGenerator, pageOptions, hasNextPage,
                hasPreviousPage, totalCount, startCursor, endCursor, _httpContextAccessor.HttpContext, RouteNames.GetCarPage, carViewModels);

            _httpContextAccessor.HttpContext.Response.Headers.Add(CustomHeaderNames.Link, paginatedResult.PageInfo.ToLinkHttpHeaderValue());

            return new OkObjectResult(paginatedResult);
        }

        private Task<List<Car>> GetCarsAsync(
            int? first,
            int? last,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken)
        {
            Task<List<Car>> getCarsTask = first.HasValue ? _carRepository.GetCarsAsync(first, createdAfter, createdBefore, cancellationToken)
                : _carRepository.GetCarsReverseAsync(last, createdAfter, createdBefore, cancellationToken);

            return getCarsTask;
        }

        private async Task<bool> GetHasNextPageAsync(
            int? first,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken)
        {
            if (first.HasValue)
            {
                return await _carRepository.GetHasNextPageAsync(first, createdAfter, cancellationToken).ConfigureAwait(false);
            }
            else if (createdBefore.HasValue)
            {
                return true;
            }

            return false;
        }

        private async Task<bool> GetHasPreviousPageAsync(
            int? last,
            DateTimeOffset? createdAfter,
            DateTimeOffset? createdBefore,
            CancellationToken cancellationToken)
        {
            if (last.HasValue)
            {
                return await _carRepository.GetHasPreviousPagAsync(last, createdBefore, cancellationToken).ConfigureAwait(false);
            }
            else if (createdAfter.HasValue)
            {
                return true;
            }

            return false;
        }
    }
}
