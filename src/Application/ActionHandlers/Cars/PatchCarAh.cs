using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
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
    public class PatchCarAh : IPatchCarAh
    {
        public PatchCarAh(ILogger<PatchCarAh> logger,
            IActionContextAccessor actionCtx,
            IMediator mediator,
            IMapper<Car, CarVm> carToCarVmMapper,
            IProblemDetailsFactory problemDetailsFactory)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _carToCarVmMapper = carToCarVmMapper ?? throw new ArgumentNullException(nameof(carToCarVmMapper));
            _pdFactory = problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
        }

        private readonly ILogger<PatchCarAh> _log;
        private readonly IActionContextAccessor _actionCtx;
        private readonly IMediator _mediator;
        private readonly IMapper<Car, CarVm> _carToCarVmMapper;
        private readonly IProblemDetailsFactory _pdFactory;

        public async Task<IActionResult> ExecuteAsync(int carId, JsonPatchDocument<CarSm> patch, CancellationToken cancellationToken)
        {
            ICommandResult<Car> result = await _mediator
                .Send(new UpdateCarCommand(carId, patch), cancellationToken);

            switch (result.Status)
            {
                case CommandStatuses.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
                case CommandStatuses.ModelInvalid:
                    ValidationProblemDetails problemDetails = _pdFactory
                        .CreateValidationProblemDetails(_actionCtx.ActionContext.HttpContext, result.ValidationResult);
                    return new BadRequestObjectResult(problemDetails);
                case CommandStatuses.NotFound:
                    return new NotFoundResult();
                case CommandStatuses.Ok:
                    CarVm carViewModel = _carToCarVmMapper.Map(result.Data);
                    return new OkObjectResult(carViewModel);
            }
        }
    }
}
