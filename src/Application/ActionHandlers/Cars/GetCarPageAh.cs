using Delobytes.AspNetCore;
using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Features.Cars.Queries;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.HttpQueryParams;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public class GetCarPageAh : IGetCarPageAh
{
    public GetCarPageAh(ILogger<GetCarPageAh> logger,
        IActionContextAccessor actionCtx,
        IMediator mediator,
        IMapper<Car, CarVm> carMapper,
        IPaginatedResultFactory paginatedResultFactory)
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _carMapper = carMapper ?? throw new ArgumentNullException(nameof(carMapper));
        _paginatedResultFactory = paginatedResultFactory ?? throw new ArgumentNullException(nameof(paginatedResultFactory));
    }

    private readonly ILogger<GetCarPageAh> _log;
    private readonly IActionContextAccessor _actionCtx;
    private readonly IMediator _mediator;
    private readonly IMapper<Car, CarVm> _carMapper;
    private readonly IPaginatedResultFactory _paginatedResultFactory;

    public async Task<IActionResult> ExecuteAsync(PageOptionsCursor pageOptions, CancellationToken cancellationToken)
    {
        DateTimeOffset? createdAfter = Cursor.FromCursor<DateTimeOffset?>(pageOptions.Before);
        DateTimeOffset? createdBefore = Cursor.FromCursor<DateTimeOffset?>(pageOptions.After);

        ICommandResult<PaginatedResult<Car>> result = await _mediator
            .Send(new GetCarPageCommand(pageOptions.First, pageOptions.Last, createdAfter, createdBefore), cancellationToken);

        switch (result.Status)
        {
            case CommandStatus.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            case CommandStatus.NotFound:
                return new NotFoundResult();
            case CommandStatus.Ok:
                PaginatedResult<Car> paginatedResult = result.Data;

                List<CarVm> carViewModels = _carMapper.MapList(paginatedResult.Items);
                (string startCursor, string endCursor) = Cursor.GetFirstAndLastCursor(paginatedResult.Items, x => x.Created);

                PaginatedResultVm<CarVm> paginatedResultVm = _paginatedResultFactory
                    .GetCursorPaginatedResult(pageOptions, paginatedResult.HasNextPage, paginatedResult.HasPreviousPage,
                    paginatedResult.TotalCount, startCursor, endCursor, RouteNames.GetCarPage, carViewModels);

                _actionCtx.ActionContext.HttpContext
                    .Response.Headers.Add(CustomHeaderNames.Link, paginatedResultVm.PageInfo.ToLinkHttpHeaderValue());

                return new OkObjectResult(paginatedResultVm);
        }
    }
}
