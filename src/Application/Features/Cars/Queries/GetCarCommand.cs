using Delobytes.AspNetCore.Application;
using Delobytes.AspNetCore.Application.Commands;
using MediatR;
using YA.ServiceTemplate.Core.Entities;

namespace YA.ServiceTemplate.Application.Features.Cars.Queries;

public class GetCarCommand : IRequest<ICommandResult<Car>>
{
    public GetCarCommand(int id)
    {
        Id = id;
    }

    public int Id { get; protected set; }

    public class GetCarkHandler : IRequestHandler<GetCarCommand, ICommandResult<Car>>
    {
        public GetCarkHandler(ILogger<GetCarkHandler> logger,
            IAppRepository carRepository)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
            _carRepository = carRepository ?? throw new ArgumentNullException(nameof(carRepository));
        }

        private readonly ILogger<GetCarkHandler> _log;
        private readonly IAppRepository _carRepository;

        public async Task<ICommandResult<Car>> Handle(GetCarCommand command, CancellationToken cancellationToken)
        {
            int carId = command.Id;

            Car car = await _carRepository.GetAsync(carId, cancellationToken);

            if (car == null)
            {
                return new CommandResult<Car>(CommandStatus.NotFound, null);
            }

            return new CommandResult<Car>(CommandStatus.Ok, car);
        }
    }
}
