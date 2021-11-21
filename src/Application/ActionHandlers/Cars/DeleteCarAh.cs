using Delobytes.AspNetCore.Application;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using YA.ServiceTemplate.Application.Features.Cars.Commands;

namespace YA.ServiceTemplate.Application.ActionHandlers.Cars;

public class DeleteCarAh : IDeleteCarAh
{
    public DeleteCarAh(ILogger<DeleteCarAh> logger,
        IActionContextAccessor actionCtx,
        IMediator mediator)
    {
        _log = logger ?? throw new ArgumentNullException(nameof(logger));
        _actionCtx = actionCtx ?? throw new ArgumentNullException(nameof(actionCtx));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    private readonly ILogger<DeleteCarAh> _log;
    private readonly IActionContextAccessor _actionCtx;
    private readonly IMediator _mediator;

    public async Task<IActionResult> ExecuteAsync(int carId, CancellationToken cancellationToken)
    {
        ICommandResult result = await _mediator
            .Send(new DeleteCarCommand(carId), cancellationToken);

        switch (result.Status)
        {
            case CommandStatus.Unknown:
            default:
                throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null);
            case CommandStatus.NotFound:
                return new NotFoundResult();
            case CommandStatus.Ok:
                return new NoContentResult();
        }
    }
}
