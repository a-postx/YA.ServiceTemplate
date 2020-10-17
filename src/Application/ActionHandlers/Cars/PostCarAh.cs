using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Features.Cars.Commands;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Constants;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars
{
    public class PostCarAh : IPostCarAh
    {
        public PostCarAh(ILogger<PostCarAh> logger,
            IActionContextAccessor actionCtx,
            IMapper<Car, CarVm> mapper,
            IMediator mediator)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
            _carVmMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        private readonly ILogger<PostCarAh> _log;
        private readonly IActionContextAccessor _actionCtx;
        private readonly IMediator _mediator;
        private readonly IMapper<Car, CarVm> _carVmMapper;

        public async Task<IActionResult> ExecuteAsync(CarSm carSm, CancellationToken cancellationToken)
        {
            ICommandResult<Car> result = await _mediator
                .Send(new CreateCarCommand(carSm), cancellationToken);

            switch (result.Status)
            {
                case CommandStatuses.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
                case CommandStatuses.Ok:
                    CarVm carVm = _carVmMapper.Map(result.Data);

                    _actionCtx.ActionContext.HttpContext
                        .Response.Headers.Add(HeaderNames.LastModified, carVm.Modified.ToString("R", CultureInfo.InvariantCulture));

                    return new CreatedAtRouteResult(RouteNames.GetCar, new { carId = carVm.CarId }, carVm);
            }
        }
    }
}
