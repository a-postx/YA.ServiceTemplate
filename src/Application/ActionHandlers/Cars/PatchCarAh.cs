using Delobytes.AspNetCore.Application;
using Delobytes.AspNetCore.Application.Actions;
using Delobytes.Mapper;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using YA.ServiceTemplate.Application.Features.Cars.Commands;
using YA.ServiceTemplate.Application.Interfaces;
using YA.ServiceTemplate.Application.Models.SaveModels;
using YA.ServiceTemplate.Application.Models.ViewModels;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public class PatchCarAh : IPatchCarAh
{
    public PatchCarAh(ILogger<PatchCarAh> logger,
        IActionContextAccessor actionCtx,
        IMediator mediator,
        IMapper<Car, CarVm> carToCarVmMapper,
        IRuntimeContextAccessor runtimeContext)
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _carToCarVmMapper = carToCarVmMapper ?? throw new ArgumentNullException(nameof(carToCarVmMapper));
        _runtimeCtx = runtimeContext ?? throw new ArgumentNullException(nameof(runtimeContext));
    }

    private readonly ILogger<PatchCarAh> _log;
    private readonly IActionContextAccessor _actionCtx;
    private readonly IMediator _mediator;
    private readonly IMapper<Car, CarVm> _carToCarVmMapper;
    private readonly IRuntimeContextAccessor _runtimeCtx;

    public async Task<IActionResult> ExecuteAsync(int carId, JsonPatchDocument<CarSm> patch, CancellationToken cancellationToken)
    {
        ICommandResult<Car> result = await _mediator
            .Send(new UpdateCarCommand(carId, patch), cancellationToken);

        switch (result.Status)
        {
            case CommandStatus.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            case CommandStatus.ModelInvalid:
                return new BadRequestObjectResult(new Failure(_runtimeCtx.GetCorrelationId(), result.ErrorMessages));
            case CommandStatus.NotFound:
                return new NotFoundResult();
            case CommandStatus.Ok:
                CarVm carViewModel = _carToCarVmMapper.Map(result.Data);
                return new OkObjectResult(carViewModel);
        }
    }
}
