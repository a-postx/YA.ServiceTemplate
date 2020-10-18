﻿using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using YA.ServiceTemplate.Application.Features.Cars.Commands;
using YA.ServiceTemplate.Application.Enums;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars
{
    public class PutCarAh : IPutCarAh
    {
        public PutCarAh(ILogger<PutCarAh> logger,
            IActionContextAccessor actionCtx,
            IMapper<Car, CarVm> mapper,
            IMediator mediator)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
            _carVmMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        private readonly ILogger<PutCarAh> _log;
        private readonly IActionContextAccessor _actionCtx;
        private readonly IMediator _mediator;
        private readonly IMapper<Car, CarVm> _carVmMapper;

        public async Task<IActionResult> ExecuteAsync(int carId, CarSm carSm, CancellationToken cancellationToken)
        {
            ICommandResult<Car> result = await _mediator
                .Send(new ReplaceCarCommand(carId, carSm), cancellationToken);

            switch (result.Status)
            {
                case CommandStatuses.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
                case CommandStatuses.NotFound:
                    return new NotFoundResult();
                case CommandStatuses.Ok:
                    CarVm carVm = _carVmMapper.Map(result.Data);
                    return new OkObjectResult(carVm);
            }
        }
    }
}