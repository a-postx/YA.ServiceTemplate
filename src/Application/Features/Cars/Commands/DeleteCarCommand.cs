using Delobytes.AspNetCore.Application;
using Delobytes.AspNetCore.Application.Commands;
using MediatR;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Commands;

public class DeleteCarCommand : IRequest<ICommandResult>
{
    public DeleteCarCommand(int id)
    {
        Id = id;
    }

    public int Id { get; protected set; }

    public class DeleteCarHandler : IRequestHandler<DeleteCarCommand, ICommandResult>
    {
        public DeleteCarHandler(ILogger<DeleteCarHandler> logger, IAppRepository carRepository)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        }

        private readonly ILogger<DeleteCarHandler> _log;
        private readonly IAppRepository _carRepository;

        public async Task<ICommandResult> Handle(DeleteCarCommand command, CancellationToken cancellationToken)
        {
            int carId = command.Id;

            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new CommandResult(CommandStatus.NotFound);
            }

            await _carRepository.DeleteAsync(car, cancellationToken);

            return new CommandResult(CommandStatus.Ok);
        }
    }
}
