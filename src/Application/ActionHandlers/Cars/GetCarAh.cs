using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Features.Cars.Queries;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars
{
    public class GetCarAh : IGetCarAh
    {
        public GetCarAh(ILogger<GetCarAh> logger,
            IActionContextAccessor actionCtx,
            IMediator mediator,
            IMapper<Car, CarVm> carMapper)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _carToVmMapper = carMapper ?? throw new ArgumentNullException(nameof(carMapper));
        }

        private readonly ILogger<GetCarAh> _log;
        private readonly IActionContextAccessor _actionCtx;
        private readonly IMediator _mediator;
        private readonly IMapper<Car, CarVm> _carToVmMapper;

        public async Task<IActionResult> ExecuteAsync(int carId, CancellationToken cancellationToken)
        {
            ICommandResult<Car> result = await _mediator
                .Send(new GetCarCommand(carId), cancellationToken);

            switch (result.Status)
            {
                case CommandStatus.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
                case CommandStatus.NotFound:
                    return new NotFoundResult();
                case CommandStatus.Ok:
                    CarVm carVm = _carToVmMapper.Map(result.Data);

                    if (_actionCtx.ActionContext.HttpContext
                        .Request.Headers.TryGetValue(HeaderNames.IfModifiedSince, out StringValues stringValues))
                    {
                        if (DateTimeOffset.TryParse(stringValues, out DateTimeOffset modifiedSince) && (modifiedSince >= carVm.Modified))
                        {
                            return new StatusCodeResult(StatusCodes.Status304NotModified);
                        }
                    }

                    _actionCtx.ActionContext.HttpContext
                        .Response.Headers.Add(HeaderNames.LastModified, carVm.Modified.ToString("R", CultureInfo.InvariantCulture));

                    return new OkObjectResult(carVm);
            }
        }
    }
}
